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

    // 【2026-09-12 20:12 日志定死】这里原先有 NavBridgeLinkWritePatch，在"顶住障碍 / 到位等待 / 翻越动画"
    // 三段挂起 BotMover.SetPlayerToNavMesh。整类删除，因为那个方法根本不是什么"重链接工具"，而是
    // bot 每帧的位移驱动本身：
    //   BotMoverImpostor.OnMotionApplied（MovementContext.OnMotionApplied 的处理器）
    //     → PositionOnWayInner += DirectionMove * deltaMove(≤0.3m)   // 虚拟点沿路径推进
    //     → SetPlayerToNavMesh(PositionOnWay)                        // BotMoverImpostor.cs:167
    //       → CastFromPos → this._owner.Transform.position = ...     // BotMover.cs:938 真正的位移写入
    // 拦掉它 = 拦掉 bot 的移动能力。同时它还是 PrevSuccessLinkedFrom 的唯一更新点（BotMover.cs:782），
    // 冻住后 method_12 会拿那个过期锚点每 0.3m 把 bot 拽回去一次：
    //   if (vector.sqrMagnitude > 0.09f)                                  // BotMover.cs:990 ⇒ 0.30m
    //       this._owner.Mover.SetPosition(this._prevSuccessLinkedFrom);   // 真实位置写入
    // 实测 20:12:38.239~20:12:54.058 共 15 次 linkBlock，bot 在距站立点 2.2~2.5m 处被钉了 15 秒，
    // 抖动幅度从未越过 0.30m（日志"两者相距" 0.00/0.21/0.24/0.15/0.29/0.28），而本轮 exec.snap = 0 次
    // ⇒ 冻结没有换来任何防瞬移收益，纯亏损。
    // 顶住阶段本来就不需要冻结：OnMotionApplied 的推进量取自实际位移 deltaMove，被障碍挡住时
    // deltaMove → 0、PositionOnWay 自动停住，不可能越过障碍去对面采样。
    // 翻越动画期的挂起也无意义：那时执行器已调 Mover.Stop() 清掉路径（BotMover.cs:561），
    // OnMotionApplied 第一行就 HavePath=false 返回，SetPlayerToNavMesh 根本不会被调用。

    // 所有 bot 传送的最终汇聚点：hop 期间拦截任何救援传送（防止下落被瞬移打断）并打印堆栈，
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

            // 跨距留痕：Teleport 不走 CastFromPos，上面那条抓不到它。
            // 正常行走的位移全部由 SetPlayerToNavMesh → CastFromPos 逐帧完成，每帧 ≤0.3m
            // （BotMoverImpostor.OnMotionApplied 的 num 上限），所以 0.6m 以上的传送一定是"救援/重链接"类写入。
            // 仍然放行（合法救援要走），只把跨距与调用方记下来，供定位"被拽回原侧"这类事件的来源
            if (botOwner != null)
            {
                var jump = (position - botOwner.Position).magnitude;
                if (jump > 0.6f)
                {
                    NavBridgeDebug.Log($"exec.teleportJump.{botOwner.Id}", $"Teleport 跨距传送 {jump:F1}m（正常每帧 ≤0.3m）：{botOwner.Position.ToString("F1")} → {position.ToString("F1")}；{Environment.StackTrace}", 0.5f);
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

            // 上面两道只覆盖 hop 与 relink，执行器的接近阶段（_hopping 为 false）原先完全放行 —— 而
            // "接近障碍时瞬移到对面"正是这个形态。这条链路本身是每帧位移驱动（≤0.3m），0.6m 以上的写入
            // 必属救援/重链接类：superFail 的 FindBetterPosition（BotMover.cs:787-796，返回
            // PrevSuccessLinkedFrom + 角点方向、不做可达性校验）或 TryExtraSample 的 2m navmesh 采样
            // （:846）。两道都在接近期被拦住（前者见 NavBridgeBetterPositionPatch，后者靠链接目标
            // 已被 NavBridgeCastPointClampPatch 拉回身体半米内而自然失效），这里是最后一道兜底：
            // 跨距过大就直接判定"这次链接没写成"。
            // 返回 fail 而不是 superFail：superFail 会接着走 FindBetterPosition + Teleport 那条路，
            // 而 fail 只是让 TryExtraSample 认为本次写入无效（它自身仍返回 true），
            // SetPlayerToNavMesh 照 extraConnect 收尾、把 _prevSuccessLinkedFrom 更新到当前位置 —— 零位移
            var owner = __instance._owner;
            if (owner != null)
            {
                var jump = (posiblePos - owner.Position).magnitude;
                if (jump > 0.6f)
                {
                    if (NavGapExecutor.IsApproaching(owner))
                    {
                        NavBridgeDebug.Log($"exec.castBlock.{owner.Id}", $"拦截接近期跨距写入 {jump:F1}m（正常每帧 ≤0.3m）：{owner.Position.ToString("F1")} → {posiblePos.ToString("F1")}；{Environment.StackTrace}", 0.5f);
                        __result = EBotLinkResult.fail;
                        return false;
                    }

                    NavBridgeDebug.Log($"exec.castJump.{owner.Id}", $"CastFromPos 跨距写入 {jump:F1}m（正常每帧 ≤0.3m）：{owner.Position.ToString("F1")} → {posiblePos.ToString("F1")}；{Environment.StackTrace}", 0.5f);
                }
            }

            return true;
        }
    }

    // 【2026-09-12 22:19 日志定死】"接近障碍时疯狂原地瞬移"的真凶。
    // FindBetterPosition 全项目只有一个调用点：SetPlayerToNavMesh 的 superFail 兜底（BotMover.cs:740）
    //   SetPosition(castPoint) → PositionOnWayInner = FindBetterPosition(castPoint) → AllowTeleport()
    //   → Teleport(PositionOnWayInner) → SetPosition(PositionOnWayInner) → CalcActionNextFrame()
    // 而这个函数本身只干一件事（BotMover.cs:787-796）：把 PrevSuccessLinkedFrom 沿
    // (CurrentCornerPoint − PrevCorner) 方向硬挪 1m，再拿 NavMesh.SamplePosition(..., 100f) 当"校验" ——
    // 半径 100m 的采样几乎必然成功，等于完全没有可达性判断。
    // bot 被执行器顶在障碍上时，PrevSuccessLinkedFrom 在障碍近侧、角点方向指向障碍另一侧
    // ⇒ 这一挪就是穿墙。实测 22:18:44~22:23:41 共 21 次位置写入（exec.snap 10 / exec.castJump 9 /
    // exec.teleportJump 2），跨距清一色 1.0~1.1m，方向正反交替（z 467.2 ↔ 466.1 来回了 5 个来回），
    // 与用户报的"疯狂原地瞬移"完全吻合；而 clamp 后它连原本的目的（贴到障碍 0.5m 内）都没达成
    // （22:19:13.873 实测仍 距=0.60）。
    // 因此在执行器接管的接近 / 到位等待 / 翻越尝试三段（这段 bot 是被我们自己顶上去的）把它压回原位：
    // 直接返回入参 castPoint，不产生任何额外位移；其余时刻（EFT 自己的救援传送）保持原样。
    // 注意：绝不能拦整条 SetPlayerToNavMesh —— 它是 bot 每帧的位移驱动本身（详见上方 20:12 说明）
    public class NavBridgeBetterPositionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), nameof(BotMover.FindBetterPosition));

        [PatchPrefix]
        public static bool Prefix(BotMover __instance, Vector3 castPoint, ref Vector3 __result)
        {
            if (!NavGapExecutor.IsApproaching(__instance._owner))
            {
                return true;
            }

            __result = castPoint;
            return false;
        }
    }

    // 【2026-09-12 22:36 日志定死】"瞬移翻越"的真凶：把 navmesh 链接目标（castPoint）拉回 bot 身上。
    // OnMotionApplied 交给 SetPlayerToNavMesh 的不是 bot 的实际位置，而是 mover 的虚拟路径点
    // PositionOnWayInner（BotMoverImpostor.cs:167 传 PositionOnWay，而 PositionOnWay 就是
    // PositionOnWayInner，BotMover.cs:206-212）：
    //   BotMoverImpostor.OnMotionApplied
    //     → PositionOnWayInner += DirectionMove * min(|deltaMove|, 0.3f)   // 虚拟点沿路径推进，:157-166
    //     → SetPlayerToNavMesh(PositionOnWay)                              // :167
    // 执行器顶住障碍时下发的推进目标是"站立点再往前 1m"（PressTarget，落在障碍面之后），于是真实 bot
    // 被碰撞体挡住、虚拟点却每帧继续朝障碍里推进 ⇒ 两者很快拉开一米以上。
    // 一旦虚拟点越过障碍，SetPlayerToNavMesh 里 0.4m 的 FindSamplePoint 就失效（BotMover.cs:719、
    // 判据 navMeshHit.position.y - castPoint.y > 0.5f，:866），落进 TryExtraSample 的 2m 兜底（:846）
    // —— 而 2m 内离虚拟点最近的 navmesh 已经是障碍对面，于是
    //   TryExtraSample → PhysicsRaycast → SetPosition → CastFromPos 直写 Transform.position（:938），
    // 一帧跨过障碍。实测 22:35:47.945 与 22:36:01.410 两次 1.1m 写入（exec.castJump / exec.snap）都是
    // 这个形态，栈清一色 TryExtraSample，且落点与执行器自己算出的"落点"重合
    // （写入 (-794.5,-59.5,467.9) vs plan 的 dest=(-794.6,-59.5,467.9)）⇒ 紧接着 alreadyAcross
    // 就把这次穿墙当成"翻越已完成"收尾了，用户看到的就是"瞬移翻越"。
    // 约束只在执行器接管的接近/到位/尝试三段生效：这三段 bot 是被我们主动顶上去的，链接目标跑到
    // 身体半米开外只可能是虚拟点失控。其余时刻（EFT 自己的救援重链接）保持原样。
    // 只改参数、不返回 false —— SetPlayerToNavMesh 本身是每帧位移驱动，绝不能挂起（详见上方 20:12 说明）
    public class NavBridgeCastPointClampPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMover), nameof(BotMover.SetPlayerToNavMesh));

        [PatchPrefix]
        public static void Prefix(BotMover __instance, ref Vector3 castPoint)
        {
            if (!NavGapExecutor.IsApproaching(__instance._owner))
            {
                return;
            }

            var body = __instance._owner.Position;
            var toCast = castPoint - body;
            toCast.y = 0f;
            if (toCast.magnitude <= 0.5f)
            {
                return;
            }

            // 只压水平分量：y 要留给 _lastGoodCastPoint 那套高度逻辑（BotMover.cs:714-717）
            var clamped = body + toCast.normalized * 0.5f;
            clamped.y = castPoint.y;
            NavBridgeDebug.Log($"exec.castClamp.{__instance._owner.Id}", $"链接目标 {castPoint.ToString("F1")} 距身体 {toCast.magnitude:F2}m > 0.5m（虚拟点失控，正常应在本帧 0.3m 以内），拉回 {clamped.ToString("F1")}", 0.5f);
            castPoint = clamped;
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