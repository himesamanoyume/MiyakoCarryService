using System;
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
    // NavGapExecutor 跨越（hop）期间挂起 EFT 的 navmesh 链接守卫，防止 bot 踏出边缘瞬间被强制写回边缘位置或瞬移到 navmesh 采样点
    public class NavBridgeMoverGuardPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), "method_12");

        [PatchPrefix]
        public static bool Prefix(BotMover __instance)
        {
            return !NavGapExecutor.IsHopping(__instance._owner);
        }
    }

    // SetPlayerToNavMesh 是位置写入的真正入口：BotMoverImpostor.OnMotionApplied 每帧绕过 method_12 直接调它，
    // FindSamplePoint 只拒更高的点 → bot 踏出边缘后采样命中下层 mesh → CastFromPos 直接写 Transform.position（瞬移根因）。
    // hop 期间跳过；执行器落地重链接（RelinkInProgress）时放行
    public class NavBridgeLinkWritePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), nameof(BotMover.SetPlayerToNavMesh));

        [PatchPrefix]
        public static bool Prefix(BotMover __instance)
        {
            return !NavGapExecutor.IsHopping(__instance._owner) || NavGapExecutor.RelinkInProgress;
        }
    }

    // 所有 bot 传送的最终汇聚点：hop 期间拦截任何救援传送（防止下落被瞬移打断）并打印堆栈，
    // 正常的 superFail Teleport 路径已随 SetPlayerToNavMesh 挂起而不可达；
    // hop 刚结束 3s 内：近距离（≤2m）传送放行（正常的落点修正），远距离（>2m）的"拉回上层边缘"拦截
    public class NavBridgeTeleportFunnelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.Teleport));

        [PatchPrefix]
        public static bool Prefix(Player __instance, Vector3 position)
        {
            var botOwner = __instance.AIData?.BotOwner;
            if (botOwner != null && NavGapExecutor.IsHopping(botOwner))
            {
                //NavBridgeDebug.Log("exec.teleportBlock", $"拦截 Teleport → {position.ToString("F1")}\n{Environment.StackTrace}");
                return false;
            }

            if (botOwner != null && NavGapExecutor.EndedHopRecently(botOwner, 3f))
            {
                var jump = (position - botOwner.Position).magnitude;
                if (jump > 2f)
                {
                    //NavBridgeDebug.Log("exec.teleportPostHopBlock", $"拦截 hop 后 {jump:F1}m 的拉回传送 → {position.ToString("F1")}\n{Environment.StackTrace}", 0.15f);
                    return false;
                }
            }

            return true;
        }
    }

    // CastFromPos 是 EFT 直写 Transform.position 的末端方法（SetPlayerToNavMesh/method_12 链路的终点）。
    // hop 期间上游均已挂起，它本不应被调用——若出现说明存在未知调用方：拦截并打印堆栈定位；
    // relink 期间仅放行 bot 附近的写入（合法重链接），远距离写入是 superFail 分支的拉回（SetPosition
    // 在 Teleport 之后无条件执行，拦 Teleport 拦不住它），同样拦截
    public class NavBridgeCastFromPosPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), nameof(BotMover.CastFromPos));

        [PatchPrefix]
        public static bool Prefix(BotMover __instance, Vector3 posiblePos, ref EBotLinkResult __result)
        {
            if (NavGapExecutor.IsHopping(__instance._owner))
            {
                //NavBridgeDebug.Log("exec.castBlock", $"拦截 CastFromPos 写位置 pos={__instance._owner.Position.ToString("F2")}\n{Environment.StackTrace}", 0.15f);
                __result = EBotLinkResult.superFail;
                return false;
            }

            if (NavGapExecutor.RelinkInProgress && __instance._owner != null
                && posiblePos.McsSqrDistance(__instance._owner.Position) > 0.5f * 0.5f)
            {
                //NavBridgeDebug.Log("exec.castBlock", $"拦截 relink 远距离写入 {posiblePos.ToString("F1")}（bot={__instance._owner.Position.ToString("F1")}）", 0.15f);
                __result = EBotLinkResult.fail;
                return false;
            }

            return true;
        }
    }

    // 跨越期间跳过障碍检测重算：RecalculateCollision 会把前方的落差当障碍（CanMove=false → walk 动画停住），
    // 跳过后 bot 保持原生行走状态走出边缘自然落下
    public class NavBridgeObstaclePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ObstacleCollisionFacade), nameof(ObstacleCollisionFacade.RecalculateCollision));

        [PatchPrefix]
        public static bool Prefix(ObstacleCollisionFacade __instance)
        {
            return !NavGapExecutor.IsHoppingNear(__instance._playerTransform.position);
        }
    }

    // 每帧无条件驱动活跃的跨越（接近/hop），不依赖大脑动作状态：
    // 护航动作因 IsCome 结束或意图切换时，跨越驱动不再断供
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