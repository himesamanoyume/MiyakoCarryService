
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
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MainMenuControllerClass), nameof(MainMenuControllerClass.method_27));

        [PatchPrefix]
        public static void Prefix(MainMenuControllerClass __instance)
        {
            __instance.RaidSettings_0.RaidMode = ERaidMode.Local;
            __instance.RaidSettings_1.Side = __instance.RaidSettings_0.Side;
            __instance.RaidSettings_1.RaidMode = ERaidMode.Local;
        }
    }

    // /// <summary>
    // /// 让Scav模式也能够调整战局设置（似乎会导致Scav模式出现投保界面）
    // /// </summary>
    // public sealed class MainMenuControllerClass2Patch : ModulePatch
    // {
    //     protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MainMenuControllerClass), nameof(MainMenuControllerClass.method_77));

    //     [PatchPrefix]
    //     public static bool Prefix(MainMenuControllerClass __instance)
    //     {
    //         __instance.method_76();
    //         __instance.method_49();
    //         if (!__instance.method_54())
    //         {
    //             return false;
    //         }

    //         __instance.method_50();
    //         return false;
    //     }
    // }
}