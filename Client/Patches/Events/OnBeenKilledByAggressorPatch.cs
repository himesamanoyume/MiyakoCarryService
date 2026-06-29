using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 借鉴fika。实现护航的击杀经验共享给老板
    /// </summary>
    public class OnBeenKilledByAggressorPatch : ModulePatch
    {
        private static CommandMgr CommandMgr => MgrAccessor.Get<CommandMgr>();

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.OnBeenKilledByAggressor));

        [PatchPostfix]
        public static void Postfix(Player __instance, IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            if (!MiyakoCarryServicePlugin.FikaInstalled || MiyakoCarryServicePlugin.IsLoadedByScriptEngine)
            {
                HandleSharedExperience(__instance, aggressor);
                HandleSharedQuestCondition(__instance, aggressor, damageInfo, bodyPart);
            }
        }

        public static void HandleSharedExperience(Player __instance, IPlayer aggressor, bool fikaSharedKillExp = false, bool fikaSharedBossExp = false)
        {
            if (CommandMgr.IsMcsMemberPlayer(aggressor.ProfileId))
            {
                var mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                if (mcsLeadPlayer == null)
                {
                    return;
                }

                if (!mcsLeadPlayer.HealthController.IsAlive)
                {
                    return;
                }

                var settings = __instance.Profile.Info.Settings;
                var countAsBoss = settings.Role.CountAsBossForStatistics() && !(settings.Role is WildSpawnType.pmcUSEC or WildSpawnType.pmcBEAR);
                var experience = settings.Experience;
                var sessionCounters = mcsLeadPlayer.Profile.EftStats.SessionCounters;

                if (experience <= 0)
                {
                    experience = Singleton<BackendConfigSettingsClass>.Instance.Experience.Kill.VictimBotLevelExp;
                }

                if (!countAsBoss)
                {
                    sessionCounters.AddLong(1L, SessionCounterTypesAbstractClass.Kills);
                    sessionCounters.AddInt(fikaSharedKillExp ? experience - experience / 2 : experience, SessionCounterTypesAbstractClass.ExpKillBase);
                }

                if (countAsBoss)
                {
                    sessionCounters.AddLong(1L, SessionCounterTypesAbstractClass.Kills);
                    sessionCounters.AddInt(fikaSharedBossExp ? experience - experience / 2 : experience, SessionCounterTypesAbstractClass.ExpKillBase);
                }
            }
        }

        public static void HandleSharedQuestCondition(Player __instance, IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, bool easyKillConditions = true)
        {
            if (!easyKillConditions)
            {
                return;
            }

            if (CommandMgr.IsMcsMemberPlayer(aggressor.ProfileId))
            {
                var mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                if (mcsLeadPlayer == null)
                {
                    return;
                }

                if (!mcsLeadPlayer.HealthController.IsAlive)
                {
                    return;
                }

                var settings = __instance.Profile.Info.Settings;
                var playerSide = __instance.Side;

                if (settings.Role != WildSpawnType.pmcBEAR)
                {
                    if (settings.Role == WildSpawnType.pmcUSEC)
                    {
                        playerSide = EPlayerSide.Usec;
                    }
                }
                else
                {
                    playerSide = EPlayerSide.Bear;
                }

                List<string> list = ["Any"];
                switch (playerSide)
                {
                    case EPlayerSide.Usec:
                        list.Add("Usec");
                        list.Add("AnyPmc");
                        list.Add("Enemy");
                        break;
                    case EPlayerSide.Bear:
                        list.Add("Bear");
                        list.Add("AnyPmc");
                        list.Add("Enemy");
                        break;
                    case EPlayerSide.Savage:
                        list.Add("Savage");
                        list.Add("Bot");
                        break;
                }

                foreach (var target in list)
                {
                    mcsLeadPlayer.AbstractQuestControllerClass.CheckKillConditionCounter(target, __instance.ProfileId, 
                        __instance.Inventory.EquippedInSlotsTemplateIds, damageInfo.Weapon, bodyPart, mcsLeadPlayer.Location, 
                        Vector3.Distance(aggressor.Position, __instance.Position), settings.Role.ToStringNoBox(), 
                        mcsLeadPlayer.CurrentHour, __instance.HealthController.BodyPartEffects, 
                        aggressor.HealthController.BodyPartEffects, __instance.TriggerZones, aggressor.HealthController.ActiveBuffsNames());
                }
            }
        }
    }
}