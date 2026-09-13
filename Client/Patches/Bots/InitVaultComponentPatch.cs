
using System.Reflection;
using EFT;
using EFT.Vaulting;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 护航开启完整骨架以允许AI进行翻越
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

    public sealed class DoVaultingTickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(VaultingComponent), nameof(VaultingComponent.DoVaultingTick));

        [PatchPrefix]
        public static bool Prefix(VaultingComponent __instance)
        {
            if (__instance._vaultingContext is not MovementContext movementContext)
            {
                return true;
            }

            var botOwner = MovementContextUtils.GetPlayer(movementContext)?.AIData?.BotOwner;
            if (botOwner == null || botOwner.IsMcsBotPlayer)
            {
                return true;
            }

            return false;
        }
    }
}