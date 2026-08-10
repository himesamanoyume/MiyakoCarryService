using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Utils
{
    internal static class SAINUtils
    {
        public const float EnterSainDist = 48f;
        public const float ExitSainDist = 52f;
        public const float EnterSainSqr = EnterSainDist * EnterSainDist;
        public const float ExitSainSqr = ExitSainDist * ExitSainDist;
        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        public static readonly Type PlayerComponentType = Type.GetType("SAIN.Components.PlayerComponent, SAIN");
        public static readonly Type SainMoverType = Type.GetType("SAIN.SAINComponent.Classes.Mover.SAINMoverClass, SAIN");
        public static readonly Type DogFightType = Type.GetType("SAIN.SAINComponent.Classes.Mover.DogFight, SAIN");
        public static readonly Type SAINEnableClassType = Type.GetType("SAIN.SAINEnableClass, SAIN");
        private static readonly Type _sainBotComponentType = GetSAINMethod?.GetParameters().ElementAtOrDefault(1)?.ParameterType?.GetElementType();
        private static readonly Type _botActivationType = _sainBotComponentType?.GetProperty("BotActivation")?.PropertyType ?? _sainBotComponentType?.GetField("BotActivation")?.FieldType;

        public static readonly MethodInfo SetTargetMoveDirectionMethod = AccessTools.Method(Type.GetType("SAIN.Classes.PlayerMovementController, SAIN"), "SetTargetMoveDirection");
        public static readonly MethodInfo RunToPointMethod = AccessTools.Method(SainMoverType, "RunToPoint");
        public static readonly MethodInfo WalkToPointMethod = AccessTools.Method(SainMoverType, "WalkToPoint");
        public static readonly MethodInfo RunToPointByWayMethod = AccessTools.Method(SainMoverType, "RunToPointByWay");
        public static readonly MethodInfo WalkToPointByWayMethod = AccessTools.Method(SainMoverType, "WalkToPointByWay");
        public static readonly MethodInfo ManualUpdateMethod = AccessTools.Method(SainMoverType, "ManualUpdate");
        public static readonly MethodInfo DogFightMoveMethod = AccessTools.Method(DogFightType, "DogFightMove");
        public static readonly MethodInfo GetSAINMethod = AccessTools.Method(SAINEnableClassType, "GetSAIN");

        private static readonly Action<object, int> _activeLayerSetter = BuildInstanceIntSetter(_botActivationType, "ActiveLayer");
        private static readonly Action<object> _manualUpdateInvoker = BuildVoidMethodInvoker(_botActivationType, "ManualUpdate");
        private static readonly Action<object, Vector3, bool, float, bool> _walkToPointInvoker = BuildWalkToPointInvoker();
        private static readonly Action<object, Vector3, bool, float, bool> _runToPointInvoker = BuildRunToPointInvoker();

        private static readonly Func<object, object> _moverBotGetter = BuildInstanceGetter(SainMoverType, "Bot");
        private static readonly Func<object, BotOwner> _botComponentBotOwnerGetter = BuildBotOwnerGetter(SainMoverType?.GetProperty("Bot")?.PropertyType ?? SainMoverType?.GetField("Bot")?.FieldType);
        private static readonly Func<object, BotOwner> _playerComponentBotOwnerGetter = BuildBotOwnerGetter(PlayerComponentType);
        private static readonly Func<object, object> _botActivationGetter = BuildInstanceGetter(_sainBotComponentType, "BotActivation");
        private static readonly Func<object, bool> _moverMovingGetter = BuildBoolGetter(SainMoverType, "Moving");
        private static readonly Func<object, BotOwner> _dogFightBotOwnerGetter = BuildBotOwnerGetter(DogFightType);

        private static readonly ConditionalWeakTable<object, BotOwner> _playerComponentBotOwners = new();
        private static readonly ConditionalWeakTable<object, BotOwner> _moverBotOwners = new();
        private static readonly ConditionalWeakTable<object, BotOwner> _dogFightBotOwners = new();

        public static bool IsMcsBotPlayer(object instance, Func<object, BotOwner> botOwnerResolver)
        {
            if (instance == null)
            {
                return false;
            }

            var botOwner = botOwnerResolver(instance);
            return botOwner != null && botOwner.IsMcsBotPlayer;
        }

        public static BotOwner GetPlayerComponentBotOwner(object playerComponent)
        {
            if (playerComponent == null)
            {
                return null;
            }

            if (_playerComponentBotOwners.TryGetValue(playerComponent, out var cached))
            {
                return cached;
            }

            var botOwner = _playerComponentBotOwnerGetter?.Invoke(playerComponent);
            if (botOwner == null)
            {
                return null;
            }

            _playerComponentBotOwners.Add(playerComponent, botOwner);
            return botOwner;
        }

        public static BotOwner GetBotOwner(object mover)
        {
            if (mover == null)
            {
                return null;
            }

            if (_moverBotOwners.TryGetValue(mover, out var cached))
            {
                return cached;
            }

            var bot = _moverBotGetter?.Invoke(mover);
            if (bot == null)
            {
                return null;
            }

            var botOwner = _botComponentBotOwnerGetter?.Invoke(bot);
            if (botOwner == null)
            {
                return null;
            }

            _moverBotOwners.Add(mover, botOwner);
            return botOwner;
        }

        public static BotOwner GetDogFightBotOwner(object dogFight)
        {
            if (dogFight == null)
            {
                return null;
            }

            if (_dogFightBotOwners.TryGetValue(dogFight, out var cached))
            {
                return cached;
            }

            var botOwner = _dogFightBotOwnerGetter?.Invoke(dogFight);
            if (botOwner == null)
            {
                return null;
            }

            _dogFightBotOwners.Add(dogFight, botOwner);
            return botOwner;
        }

        public static bool GetMoving(object mover)
        {
            return _moverMovingGetter?.Invoke(mover) ?? true;
        }

        public static void WalkToPoint(object mover, Vector3 point)
        {
            _walkToPointInvoker?.Invoke(mover, point, false, -1f, true);
        }

        private static Func<object, object> BuildInstanceGetter(Type type, string member)
        {
            if (type == null)
            {
                return null;
            }

            try
            {
                var param = Expression.Parameter(typeof(object), "o");
                var cast = Expression.Convert(param, type);
                var access = (Expression)(type.GetProperty(member) != null ? Expression.Property(cast, member) : Expression.Field(cast, member));
                var box = Expression.Convert(access, typeof(object));
                return Expression.Lambda<Func<object, object>>(box, param).Compile();
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return null;
            }
        }

        private static Action<object, int> BuildInstanceIntSetter(Type type, string member)
        {
            if (type == null)
            {
                return null;
            }

            try
            {
                var instance = Expression.Parameter(typeof(object), "o");
                var value = Expression.Parameter(typeof(int), "v");

                var prop = type.GetProperty(member);
                var field = prop == null ? type.GetField(member) : null;
                var memberType = prop != null ? prop.PropertyType : field.FieldType;

                var target = Expression.Convert(instance, type);
                var memberAccess = prop != null ? Expression.Property(target, prop) : Expression.Field(target, field);

                var assign = Expression.Assign(memberAccess, Expression.Convert(value, memberType));
                return Expression.Lambda<Action<object, int>>(assign, instance, value).Compile();
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return null;
            }
        }

        private static Action<object> BuildVoidMethodInvoker(Type type, string method)
        {
            if (type == null)
            {
                return null;
            }

            try
            {
                var methodInfo = AccessTools.Method(type, method);
                if (methodInfo == null)
                {
                    return null;
                }

                var instance = Expression.Parameter(typeof(object), "o");
                var call = Expression.Call(Expression.Convert(instance, type), methodInfo);
                return Expression.Lambda<Action<object>>(call, instance).Compile();
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return null;
            }
        }

        private static Func<object, BotOwner> BuildBotOwnerGetter(Type type)
        {
            if (type == null)
            {
                return null;
            }

            try
            {
                var param = Expression.Parameter(typeof(object), "o");
                var cast = Expression.Convert(param, type);
                var access = (Expression)(type.GetProperty("BotOwner") != null ? Expression.Property(cast, "BotOwner") : Expression.Field(cast, "BotOwner"));
                return Expression.Lambda<Func<object, BotOwner>>(Expression.Convert(access, typeof(BotOwner)), param).Compile();
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return null;
            }
        }

        private static Func<object, bool> BuildBoolGetter(Type type, string member)
        {
            if (type == null)
            {
                return null;
            }

            try
            {
                var param = Expression.Parameter(typeof(object), "o");
                var cast = Expression.Convert(param, type);
                var access = (Expression)(type.GetProperty(member) != null ? Expression.Property(cast, member) : Expression.Field(cast, member));
                return Expression.Lambda<Func<object, bool>>(access, param).Compile();
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return null;
            }
        }

        private static Action<object, Vector3, bool, float, bool> BuildWalkToPointInvoker()
        {
            if (WalkToPointMethod == null)
            {
                return null;
            }

            try
            {
                var instance = Expression.Parameter(typeof(object), "inst");
                var point = Expression.Parameter(typeof(Vector3), "point");
                var mustHaveCompletePath = Expression.Parameter(typeof(bool), "mustHaveCompletePath");
                var reachDist = Expression.Parameter(typeof(float), "reachDist");
                var checkSameWay = Expression.Parameter(typeof(bool), "checkSameWay");

                var call = Expression.Call(Expression.Convert(instance, SainMoverType), WalkToPointMethod, point, mustHaveCompletePath, reachDist, checkSameWay);
                return Expression.Lambda<Action<object, Vector3, bool, float, bool>>(call, instance, point, mustHaveCompletePath, reachDist, checkSameWay).Compile();
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return null;
            }
        }

        private static Action<object, Vector3, bool, float, bool> BuildRunToPointInvoker()
        {
            if (RunToPointMethod == null)
            {
                return null;
            }

            try
            {
                var instance = Expression.Parameter(typeof(object), "inst");
                var point = Expression.Parameter(typeof(Vector3), "point");
                var mustHaveCompletePath = Expression.Parameter(typeof(bool), "mustHaveCompletePath");
                var reachDist = Expression.Parameter(typeof(float), "reachDist");
                var checkSameWay = Expression.Parameter(typeof(bool), "checkSameWay");

                var call = Expression.Call(Expression.Convert(instance, SainMoverType), RunToPointMethod, point, mustHaveCompletePath, reachDist, checkSameWay);
                return Expression.Lambda<Action<object, Vector3, bool, float, bool>>(call, instance, point, mustHaveCompletePath, reachDist, checkSameWay).Compile();
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return null;
            }
        }

        public static bool ShouldStandStill(BotOwner botOwner)
        {
            if (botOwner == null)
            {
                return false;
            }
            if (!McsMgr.IsMcsBotPlayer(botOwner.ProfileId))
            {
                return false;
            }
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                return false;
            }
            return mcsBotPlayerData.HasDecision(Decisions.ShouldHoldPosition) || mcsBotPlayerData.HasDecision(Decisions.ShouldKeepFormation) || mcsBotPlayerData.HasDecision(Decisions.ShouldFollowMe);
        }

        public static bool ShouldRedirect(BotOwner botOwner, out McsBotPlayerData mcsBotPlayerData)
        {
            mcsBotPlayerData = null;
            if (botOwner == null || !botOwner.IsMcsBotPlayer)
            {
                return false;
            }
            mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                return false;
            }
            return mcsBotPlayerData.HasDecision(Decisions.ShouldFollowMe) || mcsBotPlayerData.HasDecision(Decisions.ShouldKeepFormation);
        }

        public static bool TryGetMoveTarget(BotOwner botOwner, McsBotPlayerData mcsBotPlayerData, out Vector3 target)
        {
            target = default;
            var mcsLeadPlayerPos = botOwner.GetMcsLeadPlayerPos(mcsBotPlayerData);
            var mcsBotPlayerConfig = mcsBotPlayerData.McsAILeadPlayer?.McsBotPlayerConfig;
            var mcsLeadPlayer = mcsBotPlayerData.LeadPlayer;

            if (mcsBotPlayerData.HasDecision(Decisions.ShouldKeepFormation) && mcsBotPlayerConfig != null && mcsBotPlayerConfig.EnableKeepFormation && mcsLeadPlayer != null)
            {
                var botIndex = Tools.GetMcsBotPlayerIndex(botOwner.ProfileId, mcsBotPlayerConfig.FormationSequentialFill);
                if (botIndex >= 5)
                {
                    var predicted = mcsLeadPlayerPos + mcsLeadPlayer.Velocity * 2f;
                    if (NavMesh.SamplePosition(predicted, out var hit, 3f, -1))
                    {
                        predicted = hit.position;
                    }
                    else
                    {
                        predicted = mcsLeadPlayerPos;
                    }

                    var formPos = Tools.ComputeTarget(mcsLeadPlayer, predicted, botIndex, Tools.ParseFormationMatrix(mcsBotPlayerConfig.FormationMatrix), mcsBotPlayerConfig.FormationSpacing);
                    if (formPos.HasValue)
                    {
                        target = formPos.Value;
                        return true;
                    }
                }
            }

            var nearPos = Tools.GetPosNearTarget(mcsLeadPlayerPos, botOwner);
            if (nearPos.HasValue)
            {
                target = nearPos.Value;
                return true;
            }

            target = mcsLeadPlayerPos;
            return true;
        }

        public static void ResetSAINLayer(BotOwner botOwner)
        {
            if (GetSAINMethod == null || botOwner == null)
            {
                return;
            }

            var parameters = new object[] { botOwner.ProfileId, null };
            GetSAINMethod.Invoke(null, parameters);
            var sainBot = parameters[1];
            if (sainBot == null)
            {
                return;
            }

            var botActivation = _botActivationGetter?.Invoke(sainBot);
            if (botActivation == null)
            {
                return;
            }

            _activeLayerSetter?.Invoke(botActivation, 0); // ESAINLayer.None = 0  
            _manualUpdateInvoker?.Invoke(botActivation);
        }

        public static void RunToPoint(object mover, Vector3 point)
        {
            _runToPointInvoker?.Invoke(mover, point, false, -1f, true);
        }
    }
}