using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    public sealed class VaultStateEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(VaultState), nameof(VaultState.Enter));

        [PatchPostfix]
        public static void Postfix(VaultState __instance)
        {
            PlayerVaultHintRecorder.OnEnter(__instance.MovementContext);
        }
    }

    public sealed class VaultStateExitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(VaultState), nameof(VaultState.Exit));

        [PatchPostfix]
        public static void Postfix(VaultState __instance)
        {
            PlayerVaultHintRecorder.OnExit(__instance.MovementContext);
        }
    }

    public sealed class ClimbStateEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ClimbState), nameof(ClimbState.Enter));

        [PatchPostfix]
        public static void Postfix(ClimbState __instance)
        {
            PlayerVaultHintRecorder.OnEnter(__instance.MovementContext);
        }
    }

    public sealed class ClimbStateExitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ClimbState), nameof(ClimbState.Exit));

        [PatchPostfix]
        public static void Postfix(ClimbState __instance)
        {
            PlayerVaultHintRecorder.OnExit(__instance.MovementContext);
        }
    }
}
