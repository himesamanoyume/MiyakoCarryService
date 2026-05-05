
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Raid
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MainMenuControllerClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MainMenuControllerClass), nameof(MainMenuControllerClass.method_27));

        [PatchPrefix]
        public static void Prefix(MainMenuControllerClass __instance)
        {
            __instance.RaidSettings_0.RaidMode = ERaidMode.Local;
            __instance.RaidSettings_1.Side = __instance.RaidSettings_0.Side;
            __instance.RaidSettings_1.RaidMode = ERaidMode.Local;
        }
    }
}