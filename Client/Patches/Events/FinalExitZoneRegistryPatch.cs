using System.Reflection;
using CommonAssets.Scripts.Cutscenes;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    public sealed class FinalExitZoneStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(FinallExitZone), nameof(FinallExitZone.Start));

        [PatchPostfix]
        public static void Postfix(FinallExitZone __instance)
        {
            InteropRegistry<FinallExitZone>.Add(__instance);
        }
    }

    public sealed class FinalExitZoneOnDestroyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(FinallExitZone), nameof(FinallExitZone.OnDestroy));

        [PatchPostfix]
        public static void Postfix(FinallExitZone __instance)
        {
            InteropRegistry<FinallExitZone>.Remove(__instance);
        }
    }
}
