
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 允许AI进行翻越（与 SAIN EnableVaultPatch / ORBIT BotVaultingPatch 同机制）：
    /// 简化骨架角色（僵尸等）不启用——翻越动画需要完整骨架
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