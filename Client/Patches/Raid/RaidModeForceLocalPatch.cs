using System.Reflection;
using EFT;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Raid
{
    /// <summary>
    /// 能够使匹配界面进入组队状态的同时不会以在线模式进行匹配
    /// </summary>
    public sealed class RaidModeForceLocalOnReadyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchmakerOperation), nameof(MatchmakerOperation.Ready));

        [PatchPrefix]
        public static void Prefix(MatchmakerOperation __instance)
        {
            if (__instance == null)
            {
                return;
            }

            var raidSettings = __instance._raidSettings;
            if (raidSettings != null)
            {
                raidSettings.RaidMode = ERaidMode.Local;
                raidSettings.IsPveOffline = true;
            }

            var offlineRaidSettings = __instance._offlineRaidSettings;
            if (offlineRaidSettings != null)
            {
                offlineRaidSettings.RaidMode = ERaidMode.Local;
                offlineRaidSettings.IsPveOffline = true;
            }
        }
    }

    /// <summary>
    /// 能够使匹配界面进入组队状态的同时不会以在线模式进行匹配
    /// </summary>
    public sealed class RaidModeForceLocalOnDecisionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(OnReadyToStartMatchingAsyncStateMachine), nameof(OnReadyToStartMatchingAsyncStateMachine.MoveNext));

        [PatchPrefix]
        public static void Prefix(OnReadyToStartMatchingAsyncStateMachine __instance)
        {
            var app = __instance?.__4__this;
            if (app == null)
            {
                return;
            }

            var raidSettings = app._raidSettings;
            if (raidSettings != null)
            {
                raidSettings.RaidMode = ERaidMode.Local;
                raidSettings.IsPveOffline = true;
            }

            var matchmaker = app._menuOperation?.Matchmaker;
            if (matchmaker == null)
            {
                return;
            }

            var matchmakerRaidSettings = matchmaker._raidSettings;
            if (matchmakerRaidSettings != null)
            {
                matchmakerRaidSettings.RaidMode = ERaidMode.Local;
                matchmakerRaidSettings.IsPveOffline = true;
            }

            var matchmakerOfflineRaidSettings = matchmaker._offlineRaidSettings;
            if (matchmakerOfflineRaidSettings != null)
            {
                matchmakerOfflineRaidSettings.RaidMode = ERaidMode.Local;
                matchmakerOfflineRaidSettings.IsPveOffline = true;
            }
        }
    }
}