using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 尝试健壮原版的GoalEnemy属性，以避免发生锁尸体的问题，并且当敌人将老板设为敌人时，直接让护航也将其设为敌人
    /// </summary>
    public sealed class SetGoalEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.PropertySetter(typeof(BotMemory), nameof(BotMemory.GoalEnemy));

        [PatchPrefix]
        public static bool Prefix(BotMemory __instance, Action<BotOwner> ___action_1, ref EnemyInfo value)
        {
            try
            {
                if (__instance.enemyInfo_0 == value)
                {
                    return false;
                }

                if (value == null || (__instance.enemyInfo_0 != value && __instance.botOwner_0.HealthController.IsAlive == true))
                {
                    __instance.botOwner_0.AimingManager.CurrentAiming.LoseTarget();
                }

                if (__instance.enemyInfo_0 != null)
                {
                    var oldPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(__instance.enemyInfo_0.Person.ProfileId);
                    if (oldPlayer != null)
                    {
                        oldPlayer.BeingHitAction -= __instance.GoalTargetBeingHitAction;
                    }
                    __instance.LastEnemy = __instance.enemyInfo_0;
                }

                var flag = __instance.enemyInfo_0 != value;
                __instance.enemyInfo_0 = value;

                if (__instance.enemyInfo_0 != null)
                {
                    var newPlayer = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(__instance.enemyInfo_0.Person.ProfileId);
                    if (newPlayer != null)
                    {
                        newPlayer.BeingHitAction += __instance.GoalTargetBeingHitAction;
                    }
                    __instance.ReportAboutEnemyToAll();
                }

                if (___action_1 != null && flag)
                {
                    ___action_1(__instance.botOwner_0);
                }

                if (__instance.enemyInfo_0 != null)
                {
                    __instance.EnemySetTime = Time.time;
                    if (!__instance.enemyInfo_0.IsVisible)
                    {
                        __instance.botOwner_0.AimingManager.CurrentAiming.LoseTarget();
                    }
                }

                if (value != null)
                {
                    __instance.DangerData.TargetNull();
                }
                else
                {
                    __instance.method_6();
                }
                return false;
            }
            finally
            {
                
            }
        }
    }
}