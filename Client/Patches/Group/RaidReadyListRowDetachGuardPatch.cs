using System.Reflection;
using EFT.UI.Matchmaker;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Group
{
    internal static class McsRowDetachGuard
    {
        internal static bool Suppress;
        internal static int Frame;
    }

    /// <summary>
    /// 只在 CleanupStalePanels 执行期间开启守卫
    /// </summary>
    public sealed class RaidReadyListCleanupScopePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RaidReadyList), nameof(RaidReadyList.CleanupStalePanels));

        [PatchPrefix]
        public static void Prefix()
        {
            McsRowDetachGuard.Suppress = true;
            McsRowDetachGuard.Frame = Time.frameCount;
        }

        [PatchPostfix]
        public static void Postfix()
        {
            McsRowDetachGuard.Suppress = false;
        }
    }

    /// <summary>
    /// 守卫期间跳过行的退订。判据同时校验帧号：跨帧后守卫自动失效，避免异常路径把守卫卡死。
    /// </summary>
    public sealed class RaidReadyPlayerPanelRemoveGuardPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RaidReadyPlayerPanel), nameof(RaidReadyPlayerPanel.remove_OnPlayerClick));

        [PatchPrefix]
        public static bool Prefix()
        {
            return !(McsRowDetachGuard.Suppress && McsRowDetachGuard.Frame == Time.frameCount);
        }
    }
}