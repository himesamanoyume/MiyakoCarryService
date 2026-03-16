using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 尝试健壮原版的GoalEnemy属性，以避免发生锁尸体的问题，并且当敌人将老板设为敌人时，直接让护航也将其设为敌人
    /// </summary>
    internal sealed class SetGoalEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.PropertySetter(typeof(BotMemoryClass), nameof(BotMemoryClass.GoalEnemy));

        private static McsMgr McsMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<McsMgr>();
            }
        }

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

                // === 修复点 1：安全移除旧事件 ===
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

                // === 修复点 2：安全添加新事件 ===
                if (__instance.EnemyInfo_0 != null)
                {
                    var newPlayer = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(__instance.EnemyInfo_0.Person.ProfileId);
                    if (newPlayer != null)
                    {
                        newPlayer.BeingHitAction += __instance.method_4;
                    }
                    __instance.method_0();
                }

                // ...其余逻辑保持不变（也建议加安全调用）
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

        [PatchPostfix]
        public static void Postfix(BotMemoryClass __instance, ref EnemyInfo value)
        {
            if (value == null)
            {
                return;
            }

            if (McsMgr.IsMcsLeadPlayer(value.Person.ProfileId))
            {
                if (!McsMgr.IsMcsBotPlayer(__instance.BotOwner_0.ProfileId))
                {
                    var mcsAILeadPlayer = McsMgr.GetMcsAILeadPlayerByMcsLeadPlayerId(value.Person.ProfileId);
                    var leadPlayer = mcsAILeadPlayer.Player() as Player;
                    if (leadPlayer.BotsGroup != null)
                    {
                        leadPlayer.BotsGroup.AddEnemy(__instance.BotOwner_0, EBotEnemyCause.AddEnemyToAllGroups);
                        leadPlayer.BotsGroup.ReportAboutEnemy(__instance.BotOwner_0, EEnemyPartVisibleType.Sence, McsMgr.GetAllMcsSquadMembersByMcsLeadId(leadPlayer.ProfileId).FirstOrDefault());
                    }
                }
            }
        }
    }
}