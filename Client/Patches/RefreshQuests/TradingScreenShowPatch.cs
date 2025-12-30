
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.RefreshQuests
{
    internal sealed class TradingScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TradingScreen), nameof(TradingScreen.Show), [typeof(TradingScreen.GClass3890)]);

        [PatchPrefix]
        public static void Prefix(TradingScreen.GClass3890 controller)
        {
            GameLoop.Instance.Session.GetDailyQuests();
        }
    }
}