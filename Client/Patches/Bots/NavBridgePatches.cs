using System.Reflection;
using EFT;
using EFT.ObstacleCollision;
using HarmonyLib;
using MiyakoCarryService.Client.Bots.Navigation;
using SPTushonka.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 用于实现护航的各种翻越、跨越边缘能力
    /// </summary>
    public class BotMoverHopGuardPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), nameof(BotMover.NavMeshLinker));

        [PatchPrefix]
        public static bool Prefix(BotMover __instance)
        {
            var botOwner = __instance._owner;
            if (botOwner == null || !NavGapExecutor.IsHopping(botOwner))
            {
                return true;
            }

            return false;
        }
    }

    public class PlayerTeleportPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.Teleport));

        [PatchPrefix]
        public static bool Prefix(Player __instance, Vector3 position)
        {
            var botOwner = __instance.AIData?.BotOwner;
            if (botOwner == null)
            {
                return true;
            }

            if (NavGapExecutor.RescueInProgress)
            {
                return true;
            }

            if (NavGapExecutor.IsHopping(botOwner))
            {
                return false;
            }

            var distance = (position - botOwner.Position).magnitude;
            if (NavGapExecutor.EndedHopRecently(botOwner) && distance > 2f)
            {
                return false;
            }

            return true;
        }
    }

    public class CastFromPosPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), nameof(BotMover.CastFromPos));

        [PatchPrefix]
        public static bool Prefix(BotMover __instance, Vector3 posiblePos, ref EBotLinkResult __result)
        {
            if (NavGapExecutor.RescueInProgress)
            {
                return true;
            }

            var owner = __instance._owner;
            if (owner == null)
            {
                return true;
            }

            if (NavGapExecutor.IsHopping(owner))
            {
                __result = EBotLinkResult.superFail;
                return false;
            }

            if (NavGapExecutor.RelinkInProgress && posiblePos.y - owner.Position.y > 0.5f)
            {
                __result = EBotLinkResult.fail;
                return false;
            }

            if (NavGapExecutor.IsApproaching(owner) && (posiblePos - owner.Position).magnitude > 0.6f)
            {
                __result = EBotLinkResult.fail;
                return false;
            }

            return true;
        }
    }

    public class FindBetterPositionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), nameof(BotMover.FindBetterPosition));

        [PatchPrefix]
        public static bool Prefix(BotMover __instance, Vector3 castPoint, ref Vector3 betterPosition, ref bool __result)
        {
            if (NavGapExecutor.RescueInProgress || !NavGapExecutor.IsApproaching(__instance._owner))
            {
                return true;
            }

            betterPosition = castPoint;
            __result = true;
            return false;
        }
    }

    public class SetPlayerToNavMeshPatch : ModulePatch
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
            var position = __instance._playerTransform.position;
            if (!NavGapExecutor.IsHoppingNear(position))
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 跨越/翻越的 tick 驱动。
    ///
    /// 5.0 里 <c>BotMover.ManualFixedUpdate</c>（以及 <c>ManualUpdate</c>）的函数体已被 IL2CPP 内联进
    /// <c>BotOwner.UpdateManual</c> / <c>BotOwner.FixedUpdate</c>：二进制中它们没有任何直接调用点
    /// （只有方法表 DATA 引用），挂在它们上面的 Harmony 补丁永远不会执行 —— 这正是 5.0 里
    /// "从不走出边缘 / 从不翻越"的根因（4.1 Mono 没有内联，所以当时能用）。
    ///
    /// <c>BotMover.MovePlayer</c> 则被 <c>BotOwner.UpdateManual</c> 与 <c>BotOwner.FixedUpdate</c> 直接调用，
    /// 是最接近原意（每帧 + 每个物理步）且确定会执行的挂点。
    /// 同帧可能被调用两次，但 <see cref="NavGapExecutor"/> 内部各项都有时间窗节流，重复调用无害。
    /// </summary>
    public class BotMoverTickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), nameof(BotMover.MovePlayer));

        [PatchPostfix]
        public static void Postfix(BotMover __instance)
        {
            var botOwner = __instance._owner;
            if (botOwner == null)
            {
                return;
            }

            NavGapExecutor.OnMoverTick(botOwner);
        }
    }
}