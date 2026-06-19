using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

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
            if (!MiyakoCarryServicePlugin.FikaInstalled)
            {
                HandleSharedExperience(__instance, aggressor);
            }
        }

        public static void HandleSharedExperience(Player __instance, IPlayer aggressor, bool sharedKillExp = true, bool sharedBossExp = true)
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

                if (sharedKillExp && !countAsBoss)
                {
                    sessionCounters.AddLong(1L, SessionCounterTypesAbstractClass.Kills);
                    sessionCounters.AddInt(experience, SessionCounterTypesAbstractClass.ExpKillBase);
                }

                if (sharedBossExp && countAsBoss)
                {
                    sessionCounters.AddLong(1L, SessionCounterTypesAbstractClass.Kills);
                    sessionCounters.AddInt(experience, SessionCounterTypesAbstractClass.ExpKillBase);
                }
            }
        }
    }
}