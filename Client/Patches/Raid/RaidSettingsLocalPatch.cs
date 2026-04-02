
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Raid
{
    /// <summary>
    /// 能够使匹配界面进入组队状态的同时不会以在线模式进行匹配
    /// </summary>
    public sealed class RaidSettingsLocalPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.PropertyGetter(typeof(RaidSettings), nameof(RaidSettings.Local));

        [PatchPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        [PatchPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref bool __result)
        {
            __result = true;
        }
    }
}