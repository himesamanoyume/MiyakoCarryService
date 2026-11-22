using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 尝试健壮原版的GoalEnemy属性，以避免发生锁尸体的问题
    /// </summary>
    [IgnoreAutoPatch]
    public sealed class SetGoalEnemyPatch : ModulePatch
    {
        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        protected override MethodBase GetTargetMethod() => AccessTools.PropertySetter(typeof(BotMemory), nameof(BotMemory.GoalEnemy));

        [PatchPrefix]
        public static bool Prefix(BotMemory __instance, ref EnemyInfo value)
        {
            try
            {
                if (value != null && value.Person != null && McsMgr.IsMcsBotPlayer(__instance._owner.ProfileId)
                    && Tools.IsForbiddenEnemy(value.Person))
                {
                    value = null;
                }

                if (value != null && (value.Person == null || value.Person.HealthController == null || !value.Person.HealthController.IsAlive))
                {
                    value = null;
                }

                if (__instance._goalEnemy == value)
                {
                    return false;
                }

                if (value == null || (__instance._goalEnemy != value && __instance._owner.HealthController.IsAlive == true))
                {
                    __instance._owner.AimingManager.CurrentAiming.LoseTarget();
                }

                if (__instance._goalEnemy != null)
                {
                    var oldPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByProfileId(__instance._goalEnemy.Person.ProfileId);
                    if (oldPlayer != null)
                    {
                        TryChangeBeingHitAction(oldPlayer, __instance.GoalTargetBeingHitAction, add: false);
                    }
                    __instance.LastEnemy = __instance._goalEnemy;
                }

                var flag = __instance._goalEnemy != value;
                __instance._goalEnemy = value;

                if (__instance._goalEnemy != null)
                {
                    var newPlayer = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(__instance._goalEnemy.Person.ProfileId);
                    if (newPlayer != null)
                    {
                        TryChangeBeingHitAction(newPlayer, __instance.GoalTargetBeingHitAction, add: true);
                    }
                    __instance.ReportAboutEnemyToAll();
                }

                var onGoalEnemyChanged = __instance.OnGoalEnemyChangedField;
                if (onGoalEnemyChanged != null && flag)
                {
                    onGoalEnemyChanged.Invoke(__instance._owner);
                }

                if (__instance._goalEnemy != null)
                {
                    __instance.EnemySetTime = Time.time;
                    if (!__instance._goalEnemy.IsVisible)
                    {
                        __instance._owner.AimingManager.CurrentAiming.LoseTarget();
                    }
                }

                if (value != null)
                {
                    __instance.DangerData.TargetNull();
                }
                else
                {
                    __instance.CheckAttackImmediately();
                }
                return false;
            }
            finally
            {
                
            }
        }

        private static bool _beingHitActionUnsupported;

        /// <summary>
        /// 5.0 的 Il2CppInterop 无法把参数含非 blittable 结构体 DamageInfo 的委托转成原生委托
        /// （会抛 "Delegate has parameter of type ... which is not supported"），
        /// 这里做一次容错：失败只警告一次并不再尝试，避免整个 SetGoalEnemy 被异常打断。
        /// </summary>
        private static void TryChangeBeingHitAction(Player player, Action<DamageInfo, EBodyPart, float> handler, bool add)
        {
            if (_beingHitActionUnsupported)
            {
                return;
            }

            try
            {
                if (add)
                {
                    player.add_BeingHitAction(handler);
                }
                else
                {
                    player.remove_BeingHitAction(handler);
                }
            }
            catch (Exception exception)
            {
                _beingHitActionUnsupported = true;
                McsLogger.LogWarning("[SetGoalEnemyPatch] add/remove_BeingHitAction 在 5.0 不可用（Il2CppInterop 不支持非 blittable 结构体参数委托），已跳过: " + exception.GetType().Name + ": " + exception.Message);
            }
        }
    }
}
