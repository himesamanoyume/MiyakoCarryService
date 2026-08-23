using System.Reflection;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.SAIN
{
    /// <summary>  
    /// 拦截 SAIN 所有基于路径的实际位移
    /// </summary>  
    public sealed class SetTargetMoveDirectionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => SAINUtils.SetTargetMoveDirectionMethod;

        [PatchPrefix]
        public static bool Prefix(object playerComp)
        {
            if (!SAINUtils.IsMcsBotPlayer(playerComp, SAINUtils.GetPlayerComponentBotOwner))
            {
                return true;
            }

            var botOwner = SAINUtils.GetPlayerComponentBotOwner(playerComp);
            if (SAINUtils.ShouldStandStill(botOwner))
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 拦截 SAIN 的 RunToPoint
    /// </summary>
    public sealed class RunToPointPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => SAINUtils.RunToPointMethod;

        [PatchPrefix]
        public static void Prefix(object __instance, ref Vector3 point)
        {
            if (!SAINUtils.IsMcsBotPlayer(__instance, SAINUtils.GetBotOwner))
            {
                return;
            }

            var botOwner = SAINUtils.GetBotOwner(__instance);
            if (SAINUtils.ShouldRedirect(botOwner, out var mcsBotPlayerData) && SAINUtils.TryGetMoveTarget(botOwner, mcsBotPlayerData, out var target))
            {
                point = target;
            }
        }
    }

    /// <summary>
    /// 拦截 SAIN 的 WalkToPoint
    /// </summary>
    public sealed class WalkToPointPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => SAINUtils.WalkToPointMethod;

        [PatchPrefix]
        public static void Prefix(object __instance, ref Vector3 point)
        {
            if (!SAINUtils.IsMcsBotPlayer(__instance, SAINUtils.GetBotOwner))
            {
                return;
            }

            var botOwner = SAINUtils.GetBotOwner(__instance);
            if (SAINUtils.ShouldRedirect(botOwner, out var mcsBotPlayerData) && SAINUtils.TryGetMoveTarget(botOwner, mcsBotPlayerData, out var target))
            {
                point = target;
            }
        }
    }

    /// <summary>
    /// 拦截 SAIN 的 RunToPointByWay
    /// </summary>
    public sealed class RunToPointByWayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => SAINUtils.RunToPointByWayMethod;

        [PatchPrefix]
        public static bool Prefix(object __instance, ref bool __result)
        {
            if (!SAINUtils.IsMcsBotPlayer(__instance, SAINUtils.GetBotOwner))
            {
                return true;
            }

            var botOwner = SAINUtils.GetBotOwner(__instance);
            if (SAINUtils.ShouldRedirect(botOwner, out _))
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 拦截 SAIN 的 WalkToPointByWay
    /// </summary>
    public sealed class WalkToPointByWayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => SAINUtils.WalkToPointByWayMethod;

        [PatchPrefix]
        public static bool Prefix(object __instance, ref bool __result)
        {
            if (!SAINUtils.IsMcsBotPlayer(__instance, SAINUtils.GetBotOwner))
            {
                return true;
            }

            var botOwner = SAINUtils.GetBotOwner(__instance);
            if (SAINUtils.ShouldRedirect(botOwner, out _))
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 拦截 SAIN 的 DogFightMove
    /// </summary>
    public sealed class DogFightMovePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => SAINUtils.DogFightMoveMethod;

        [PatchPrefix]
        public static bool Prefix(object __instance)
        {
            if (!SAINUtils.IsMcsBotPlayer(__instance, SAINUtils.GetBotOwner))
            {
                return true;
            }

            var botOwner = SAINUtils.GetDogFightBotOwner(__instance);
            return !SAINUtils.ShouldRedirect(botOwner, out _);
        }
    }

    /// <summary>
    /// 使护航在 SAIN 激活时继续维持应有的站位
    /// </summary>
    public sealed class MoverManualUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => SAINUtils.ManualUpdateMethod;

        [PatchPostfix]
        public static void Postfix(object __instance)
        {
            if (!SAINUtils.IsMcsBotPlayer(__instance, SAINUtils.GetBotOwner))
            {
                return;
            }

            var botOwner = SAINUtils.GetBotOwner(__instance);
            if (!SAINUtils.ShouldRedirect(botOwner, out var mcsBotPlayerData))
            {
                return;
            }

            if (SAINUtils.GetMoving(__instance))
            {
                return;
            }

            if (!SAINUtils.TryGetMoveTarget(botOwner, mcsBotPlayerData, out var target))
            {
                return;
            }

            if (botOwner.Position.McsSqrDistance(target) <= 1.5f * 1.5f)
            {
                return;
            }

            var sqrToTarget = botOwner.Position.McsSqrDistance(target);
            if (sqrToTarget <= 1.5f * 1.5f)
            {
                return;
            }

            var mcsBotPlayerConfig = mcsBotPlayerData.McsAILeadPlayer?.McsBotPlayerConfig;
            var isKeepFormation = mcsBotPlayerData.HasIntent(Intents.ShouldKeepFormation) && mcsBotPlayerConfig != null && mcsBotPlayerConfig.EnableKeepFormation;

            if (isKeepFormation && sqrToTarget >= 64f)
            {
                SAINUtils.RunToPoint(__instance, target);
            }
            else
            {
                SAINUtils.WalkToPoint(__instance, target);
            }
        }
    }
}