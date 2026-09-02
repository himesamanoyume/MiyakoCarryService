using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 实现阻止护航说话
    /// </summary>
    public sealed class PlayerSayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.Say));

        private static SubtitlesMgr SubtitlesMgr => field ??= MgrAccessor.Get<SubtitlesMgr>();

        [PatchPrefix]
        public static bool Prefix(Player __instance)
        {
            if (SubtitlesMgr.ShouldSilenceBot(__instance))
            {
                return false;
            }

            return true;
        }
    }
}