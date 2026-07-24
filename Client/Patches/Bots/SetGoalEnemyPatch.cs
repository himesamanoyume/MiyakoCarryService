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
        protected override MethodBase GetTargetMethod() => AccessTools.PropertySetter(typeof(BotMemory), nameof(BotMemory.GoalEnemy));

        [PatchPrefix]
        public static bool Prefix(BotMemory __instance, Action<BotOwner> ____onGoalEnemyChanged, ref EnemyInfo value)
        {
            try
            {
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
                    var oldPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(__instance._goalEnemy.Person.ProfileId);
                    if (oldPlayer != null)
                    {
                        oldPlayer.BeingHitAction -= __instance.GoalTargetBeingHitAction;
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
                        newPlayer.BeingHitAction += __instance.GoalTargetBeingHitAction;
                    }
                    __instance.ReportAboutEnemyToAll();
                }

                if (____onGoalEnemyChanged != null && flag)
                {
                    ____onGoalEnemyChanged(__instance._owner);
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