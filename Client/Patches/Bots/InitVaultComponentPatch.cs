
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 允许AI进行翻越
    /// </summary>
    public sealed class InitVaultComponentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.InitVaultingComponent));

        [PatchPrefix]
        public static void Prefix(Player __instance, ref bool aiControlled)
        {
            if (__instance.UsedSimplifiedSkeleton)
            {
                return;
            }

            aiControlled = false;
        }
    }
}