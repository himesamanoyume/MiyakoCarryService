
using System.Reflection;
using EFT;
using EFT.UI.Matchmaker;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Raid
{
    /// <summary>
    /// 能够使匹配界面进入组队状态的同时不会以在线模式进行匹配
    /// </summary>
    internal sealed class RaidSettingsLocalPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.PropertyGetter(typeof(RaidSettings), nameof(RaidSettings.Local));

        [PatchPrefix]
        public static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    /// <summary>
    /// 使匹配界面的准备按钮在进行了RaidSettingsLocalPatch后仍可以点击
    /// </summary>
    internal sealed class MatchMakerAcceptScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchMakerAcceptScreen), nameof(MatchMakerAcceptScreen.method_16));

        [PatchPrefix]
        public static void Prefix(ref EMatchingStatus matchingStatus)
        {
            matchingStatus = EMatchingStatus.Ready;
        }
    }
}