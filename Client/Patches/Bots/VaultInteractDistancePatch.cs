using System.Reflection;
using EFT.Vaulting;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    public sealed class VaultInteractDistancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(VaultMoveModel), nameof(VaultMoveModel.CheckVaultCondition));

        [PatchPrefix]
        public static void Prefix(VaultMoveModel __instance, ref float distance)
        {
            InteractDistanceNudge.Nudge(__instance._settings?.MoveRestrictions, ref distance);
        }
    }

    public sealed class VaultAutoInteractDistancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(VaultMoveModel), nameof(VaultMoveModel.CheckAutoVaultCondition));

        [PatchPrefix]
        public static void Prefix(VaultMoveModel __instance, ref float distance)
        {
            InteractDistanceNudge.Nudge(__instance._settings?.AutoMoveRestrictions, ref distance);
        }
    }

    public sealed class ClimbInteractDistancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ClimbMoveModel), nameof(ClimbMoveModel.CheckClimbCondition));

        [PatchPrefix]
        public static void Prefix(ClimbMoveModel __instance, ref float distance)
        {
            InteractDistanceNudge.Nudge(__instance._settings?.MoveRestrictions, ref distance);
        }
    }

    public sealed class ClimbAutoInteractDistancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ClimbMoveModel), nameof(ClimbMoveModel.CheckAutoClimbCondition));

        [PatchPrefix]
        public static void Prefix(ClimbMoveModel __instance, ref float distance)
        {
            InteractDistanceNudge.Nudge(__instance._settings?.AutoMoveRestrictions, ref distance);
        }
    }
}