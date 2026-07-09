
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Raid
{
    /// <summary>
    /// 修复使用RaidSettingsLocalPatch后战局设置不生效的问题
    /// </summary>
    public sealed class MainMenuControllerClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MainMenuShowOperation), nameof(MainMenuShowOperation.method_27));

        [PatchPrefix]
        public static void Prefix(MainMenuShowOperation __instance)
        {
            __instance.raidSettings_0.RaidMode = ERaidMode.Local;
            __instance.raidSettings_1.Side = __instance.raidSettings_0.Side;
            __instance.raidSettings_1.RaidMode = ERaidMode.Local;
        }
    }
}