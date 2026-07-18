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
    /// 尝试健壮原版的GoalEnemy属性，以避免发生锁尸体的问题
    /// </summary>
    public sealed class SetGoalEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.PropertySetter(typeof(BotMemoryClass), nameof(BotMemoryClass.GoalEnemy));

        [PatchPrefix]
        public static bool Prefix(BotMemoryClass __instance, Action<BotOwner> ___action_1, ref EnemyInfo value)
        {
            try
            {
                if (__instance.EnemyInfo_0 == value)
                {
                    return false;
                }

                if (value == null || (__instance.EnemyInfo_0 != value && __instance.BotOwner_0.HealthController.IsAlive == true))
                {
                    __instance.BotOwner_0.AimingManager.CurrentAiming.LoseTarget();
                }

                if (__instance.EnemyInfo_0 != null)
                {
                    var oldPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(__instance.EnemyInfo_0.Person.ProfileId);
                    if (oldPlayer != null)
                    {
                        oldPlayer.BeingHitAction -= __instance.method_4;
                    }
                    __instance.LastEnemy = __instance.EnemyInfo_0;
                }

                var flag = __instance.EnemyInfo_0 != value;
                __instance.EnemyInfo_0 = value;

                if (__instance.EnemyInfo_0 != null)
                {
                    var newPlayer = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(__instance.EnemyInfo_0.Person.ProfileId);
                    if (newPlayer != null)
                    {
                        newPlayer.BeingHitAction += __instance.method_4;
                    }
                    __instance.method_0();
                }

                if (___action_1 != null && flag)
                {
                    ___action_1(__instance.BotOwner_0);
                }

                if (__instance.EnemyInfo_0 != null)
                {
                    __instance.EnemySetTime = Time.time;
                    if (!__instance.EnemyInfo_0.IsVisible)
                    {
                        __instance.BotOwner_0.AimingManager.CurrentAiming.LoseTarget();
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