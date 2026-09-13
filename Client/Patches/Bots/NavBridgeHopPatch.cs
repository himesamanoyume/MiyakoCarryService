using System.Reflection;
using EFT;
using EFT.ObstacleCollision;
using HarmonyLib;
using MiyakoCarryService.Client.Bots.Navigation;
using MiyakoCarryService.Client.Extensions;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    public class NavBridgeMoverGuardPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), "method_12");

        [PatchPrefix]
        public static bool Prefix(BotMover __instance)
        {
            return !NavGapExecutor.IsHopping(__instance._owner);
        }
    }

    public class NavBridgeTeleportFunnelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.Teleport));

        [PatchPrefix]
        public static bool Prefix(Player __instance, Vector3 position)
        {
            var botOwner = __instance.AIData?.BotOwner;
            if (botOwner != null && NavGapExecutor.RescueInProgress)
            {
                return true;
            }

            if (botOwner != null && NavGapExecutor.IsHopping(botOwner))
            {
                return false;
            }

            if (botOwner != null && NavGapExecutor.EndedHopRecently(botOwner, 3f)
                && (position - botOwner.Position).magnitude > 2f)
            {
                return false;
            }

            return true;
        }
    }

    public class NavBridgeCastFromPosPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), nameof(BotMover.CastFromPos));

        [PatchPrefix]
        public static bool Prefix(BotMover __instance, Vector3 posiblePos, ref EBotLinkResult __result)
        {
            if (NavGapExecutor.RescueInProgress)
            {
                return true;
            }

            if (NavGapExecutor.IsHopping(__instance._owner))
            {
                __result = EBotLinkResult.superFail;
                return false;
            }

            if (NavGapExecutor.RelinkInProgress && __instance._owner != null
                && posiblePos.McsSqrDistance(__instance._owner.Position) > 0.5f * 0.5f)
            {
                __result = EBotLinkResult.fail;
                return false;
            }

            var owner = __instance._owner;
            if (owner != null && NavGapExecutor.IsApproaching(owner) && (posiblePos - owner.Position).magnitude > 0.6f)
            {
                __result = EBotLinkResult.fail;
                return false;
            }

            return true;
        }
    }

    public class NavBridgeBetterPositionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), nameof(BotMover.FindBetterPosition));

        [PatchPrefix]
        public static bool Prefix(BotMover __instance, Vector3 castPoint, ref Vector3 __result)
        {
            if (NavGapExecutor.RescueInProgress || !NavGapExecutor.IsApproaching(__instance._owner))
            {
                return true;
            }

            __result = castPoint;
            return false;
        }
    }

    public class NavBridgeCastPointClampPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), nameof(BotMover.SetPlayerToNavMesh));

        [PatchPrefix]
        public static void Prefix(BotMover __instance, ref Vector3 castPoint)
        {
            if (NavGapExecutor.RescueInProgress || !NavGapExecutor.IsApproaching(__instance._owner))
            {
                return;
            }

            var body = __instance._owner.Position;
            var toCast = castPoint - body;
            toCast.y = 0f;
            if (toCast.magnitude <= 0.3f)
            {
                return;
            }

            var clamped = body + toCast.normalized * 0.3f;
            clamped.y = castPoint.y;
            castPoint = clamped;
        }
    }

    public class NavBridgeObstaclePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ObstacleCollisionFacade), nameof(ObstacleCollisionFacade.RecalculateCollision));

        [PatchPrefix]
        public static bool Prefix(ObstacleCollisionFacade __instance)
        {
            return !NavGapExecutor.IsHoppingNear(__instance._playerTransform.position);
        }
    }

    public class NavBridgeMoverDrivePostfix : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), nameof(BotMover.ManualFixedUpdate));

        [PatchPostfix]
        public static void Postfix(BotMover __instance)
        {
            NavGapExecutor.OnMoverTick(__instance._owner);
        }
    }
}