using System.Collections.Generic;
using EFT;
using EFT.Vaulting;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Navigation
{
    public class NavGapExecutor
    {
        private static readonly Dictionary<BotOwner, NavGapExecutor> CrossingBots = new Dictionary<BotOwner, NavGapExecutor>();

        // 跨越结束后 3s 内的 bot（Funnel 补丁据此拦截远距离拉回传送）
        private static readonly Dictionary<BotOwner, float> RecentHopEnds = new Dictionary<BotOwner, float>();

        // 未进入翻越动画就放弃的 bot → 放弃时刻。Cancel() 只清得掉执行器自己的状态，清不掉 EFT 侧
        // 那条顶住的单点路径：虚拟点 PositionOnWay 推的是"路径进度"、不受碰撞体阻挡，还会沿
        // "障碍面之后 1m"继续前进，一旦越过障碍就走 TryExtraSample 的 2m 兜底采样把 bot 一帧送到对面
        // —— 实测 23:06:29.767 的 1.1m 穿墙正好发生在第二次尝试失败 Cancel() 之后 0.06s
        //（23:06:28.906 第一次尝试失败 → +0.8s 第二次失败 → _vaultTryCount >= 2 → 拉黑 + Cancel）。
        // 所以放弃后的短暂窗口内继续按"接管中"对待（IsApproaching），并在 Cancel() 里把那条
        // 残留路径直接清掉（Mover.Stop()，与翻越成功时同一处理）
        private static readonly Dictionary<BotOwner, float> RecentAborts = new Dictionary<BotOwner, float>();

        private const float AbortGuardDuration = 1.5f;

        // 全局单帧位移侦查用的上帧位置。正常行走每帧位移上限 0.3m（OnMotionApplied 的 num =
        // min(deltaMove.magnitude, 0.3f)，BotMoverImpostor.cs:109-113），远小于 0.6m 阈值
        // ⇒ 单帧位移超过 0.6m 必为写位置事件（CastFromPos / Teleport 直写 Transform）。
        //
        // 关键是不能只盯跨越中的 bot：上面几个补丁的拦截条件全是 IsHopping()，而 IsHopping 只看 _hopping——
        // 执行器的"接近阶段"（_hopping 仍为 false）里 SetPlayerToNavMesh 是放行的，跨越被 Cancel 之后
        // 更是彻底没人看着。所以这里按 bot 无条件记录，覆盖全部阶段
        private static readonly Dictionary<BotOwner, Vector3> LastMoverPositions = new Dictionary<BotOwner, Vector3>();

        // 翻越失败黑名单（全局：翻越可行性是几何属性，与具体 bot 无关）
        private static readonly List<VaultBlacklistEntry> VaultBlacklist = new List<VaultBlacklistEntry>();

        private struct VaultBlacklistEntry
        {
            public Vector3 Pos;
            public float Until;
            public int Fails;
        }

        private const float VaultBlacklistRadius = 1.5f;
        private const float VaultBlacklistDuration = 60f;
        private const int VaultBlacklistFailures = 3;

        private Vector3 _edge;
        private Vector3 _destination;
        private BotOwner _botOwner;
        private float _startTime;
        private bool _hopping;
        private ENavGapType _type;
        private bool _arrived;
        private int _vaultTryCount;
        private float _nextVaultTryTime;

        // 翻越动画开始（_hopping 置真）的时刻：判定"被抬到障碍顶上卡住"要用它做时基，
        // 不能用 _startTime（那是接近阶段的起点，接近本身可能已经花掉好几秒）
        private float _hopStartTime;

        // 到位（_arrived 置真）的时刻：动画过渡等待要用它做上限，不能用 _startTime
        // （接近本身可能花掉好几秒，用 _startTime 会让等待窗口被接近耗时吃掉）
        private float _arrivedTime;

        // 当前是否在用"直线顶上去"推进（推进目标被放到障碍另一侧）。仅用于日志区分
        // "正常寻路" 与 "直线顶住" 两种接近形态，不再参与任何位置写入的拦截
        private bool _pushingIntoObstacle;

        // 接近阶段的最近最好成绩与下一条卡住取证日志的时间：距离不再缩短时才取证
        private float _bestApproachDistance;
        private float _nextStallLogTime;

        // 上一次 Begin 的站立点，用来判断"这次规划是不是同一个障碍"。
        // 规划器每秒都可能重进 Begin（详见 Begin 里 sameGap 的说明），重进时若把失败额度清零，
        // "两次失败即拉黑 60s"就永远触发不了，bot 会被一直按在同一个障碍上
        private Vector3 _lastGapNear;
        private bool _hasGap;

        // 【2026-09-13 新增】"离开 navmesh 且卡在几何里"的自救。
        // 翻越把 bot 送进封闭几何（车厢、檐口）之后，原先只有两条出路：stuckOnTop（必须还在动画期、
        // 2.5s 无位移、且脚下 0.5m 内没有 navmesh，条件极窄）与 EFT 自己的 SetPlayerToNavMesh superFail
        // / 卡死 15s 后由 ForceTeleportCommandAction 传走（实测跨距 18.9~111.9m，用户看到的瞬移）。
        // 实测位置几乎不动 ≥8s 的窗口有 7 段（最长 36.1s），全部集中在 x≈-770..-738 / z≈460..469 的车堆
        // —— 用户描述"翻进车里出不来、只能手动传送"就是这个形态。
        // 这里按 bot 记录"最后一次确认站在 navmesh 上的位置"，若它连续 3s 位移不足 0.5m 且当前不在
        // navmesh 上，就把它拉回那个已知良好点（几米内、有界），不再等 EFT 那个又慢又远的传送
        private static readonly Dictionary<BotOwner, RescueWatch> RescueWatches = new Dictionary<BotOwner, RescueWatch>();

        private sealed class RescueWatch
        {
            public Vector3 LastGoodPos;   // 最后一次确认可达的网格点
            public bool HasLastGood;
            public Vector3 Anchor;        // 判"没动"用的锚点
            public float AnchorTime;
            public float NextProbeTime;   // navmesh 采样的节流时刻
        }

        // 顶住判据用：锚点位置与其最后一次变化的时间（0.4s 无位移 ⇒ 被障碍挡住）
        private Vector3 _pressAnchor;
        private float _pressAnchorTime;

        // 直线推进目标的下发节流（每帧重建单点路径会一直重置 mover 的路径进度）
        private float _nextPressTime;

        public bool IsCrossing { get; private set; }

        // 跨越超时（大概率边缘不可跨越，如护栏/檐口）后的一段时间内不再重复规划
        public float NextPlanCooldownUntil { get; private set; }

        // 执行器落地重链接主动调用 SetPlayerToNavMesh 时置位，NavBridgeCastFromPosPatch 据此只放行
        // bot 附近的写入（0.5m 内），远距离写入是 superFail 分支的拉回
        public static bool RelinkInProgress;

        // 自救看门狗把 bot 拉回网格点时置位。位置写入的几道护栏（clamp / FindBetterPosition / castBlock）
        // 全部以"执行器接管中"为前提，而自救恰恰可能发生在放弃后的 1.5s 窗口内（IsApproaching 为真）
        // —— 那种时候护栏会把这次合法救援也一起拦掉，所以自救期间必须整体放行
        public static bool RescueInProgress;

        public bool IsHoppingNow => _hopping;

        public static bool IsHopping(BotOwner botOwner)
        {
            return botOwner != null && CrossingBots.TryGetValue(botOwner, out var executor) && executor._hopping;
        }

        // 执行器接管了这个 bot、且还没进翻越动画（接近 / 到位等待 / 翻越尝试三段）。
        // 这三段里 bot 是被我们主动"顶"到障碍上的：SetPlayerToNavMesh 的 superFail 兜底会把
        // PrevSuccessLinkedFrom 沿当前角点方向硬挪 1m（FindBetterPosition，BotMover.cs:787-796），
        // 也就是把 bot 从障碍近侧直接甩到另一侧。实测 22:18:44~22:23:41 的 21 次 1.0~1.1m 位置写入
        // 全部落在这一段，且方向正反交替 —— 这就是"接近障碍时疯狂原地瞬移"
        // （拦截点见 NavBridgeBetterPositionPatch）
        //
        // 【2026-09-12 23:06 补】放弃后的 1.5s 内也算"接管中"：Cancel() 清不掉 EFT 侧的顶住路径，
        // 虚拟点还会继续朝障碍里推进，穿墙就发生在放弃之后（实测 0.06s）。窗口长度取 1.5s ——
        // 远大于实测的 0.06s，而窗口内护栏只对"链接目标领先身体 >0.3m"的失控生效（正常行走时
        // 每帧推进量 ≤0.3m，见 OnMotionApplied），所以对放弃后正常走绕路没有影响
        public static bool IsApproaching(BotOwner botOwner)
        {
            if (botOwner == null)
            {
                return false;
            }

            if (CrossingBots.TryGetValue(botOwner, out var executor))
            {
                return !executor._hopping;
            }

            return RecentAborts.TryGetValue(botOwner, out var abortedAt) && Time.time - abortedAt < AbortGuardDuration;
        }

        // 【2026-09-12 20:12 日志定死】这里原先有 IsLinkWriteFrozen / LinkWriteFrozen，用来在
        // "顶住障碍 / 到位等待 / 翻越动画" 三段挂起 BotMover.SetPlayerToNavMesh。整段删除，原因是
        // 那个方法根本不是"重链接工具"，而是 bot 每帧的位移驱动本身：
        //   BotMoverImpostor.OnMotionApplied（MovementContext.OnMotionApplied 的处理器）
        //     → PositionOnWayInner += DirectionMove * deltaMove(≤0.3m)   // 虚拟点沿路径推进
        //     → SetPlayerToNavMesh(PositionOnWay)                        // BotMoverImpostor.cs:167
        //       → CastFromPos → this._owner.Transform.position = ...     // BotMover.cs:938 真正的位移写入
        // 拦掉它 = 拦掉 bot 的移动能力。同时它还是 PrevSuccessLinkedFrom 的唯一更新点（BotMover.cs:782），
        // 冻住后 method_12 会拿那个过期锚点每 0.3m 把 bot 拽回去一次：
        //   if (vector.sqrMagnitude > 0.09f)                              // BotMover.cs:990 ⇒ 0.30m
        //       this._owner.Mover.SetPosition(this._prevSuccessLinkedFrom);   // 真实位置写入
        // 实测 20:12:38.239~20:12:54.058 共 15 次 linkBlock，bot 在距站立点 2.2~2.5m 处被钉了 15 秒，
        // 抖动幅度从未越过 0.30m（日志"两者相距" 0.00/0.21/0.24/0.15/0.29/0.28），且本轮 exec.snap = 0 次
        // ⇒ 冻结没有换来任何防瞬移收益，纯亏损。
        // 顶住阶段本来就不需要冻结：OnMotionApplied 的推进量取自实际位移 deltaMove，被障碍挡住时
        // deltaMove → 0，PositionOnWay 自动停住，不会越过障碍去对面采样。

        // hop 结束后 within 秒内返回 true（Funnel 补丁的拉回拦截窗口）
        public static bool EndedHopRecently(BotOwner botOwner, float within)
        {
            return botOwner != null && RecentHopEnds.TryGetValue(botOwner, out var time) && Time.time - time < within;
        }

        // 障碍检测系统按位置匹配跨越中的 bot（ObstacleCollisionFacade 只有 Transform，没有 bot 引用）
        public static bool IsHoppingNear(Vector3 pos)
        {
            foreach (var executor in CrossingBots.Values)
            {
                if (executor._hopping && executor._botOwner != null && executor._botOwner.Position.McsSqrDistance(pos) <= 1f)
                {
                    return true;
                }
            }

            return false;
        }

        // 翻越黑名单查询：近期在该障碍上失败过则跳过（直接走寻路的绕路结果）
        public static bool IsVaultBlacklisted(Vector3 pos)
        {
            VaultBlacklist.RemoveAll(e => e.Until > 0f && Time.time >= e.Until);
            foreach (var entry in VaultBlacklist)
            {
                if (entry.Until > 0f && entry.Pos.McsSqrDistance(pos) <= VaultBlacklistRadius * VaultBlacklistRadius)
                {
                    return true;
                }
            }

            return false;
        }

        // 记一次翻越失败；immediate=true（执行器两次尝试失败）立即生效，否则累计到阈值
        public static void BlacklistVault(Vector3 pos, bool immediate)
        {
            VaultBlacklist.RemoveAll(e => e.Until > 0f && Time.time >= e.Until);
            for (var i = 0; i < VaultBlacklist.Count; i++)
            {
                if (VaultBlacklist[i].Pos.McsSqrDistance(pos) <= VaultBlacklistRadius * VaultBlacklistRadius)
                {
                    var entry = VaultBlacklist[i];
                    entry.Fails++;
                    if (immediate || entry.Fails >= VaultBlacklistFailures)
                    {
                        entry.Until = Time.time + VaultBlacklistDuration;
                    }
                    VaultBlacklist[i] = entry;
                    return;
                }
            }

            VaultBlacklist.Add(new VaultBlacklistEntry
            {
                Pos = pos,
                Until = immediate ? Time.time + VaultBlacklistDuration : 0f,
                Fails = 1,
            });
        }

        // 挂接在 BotMover.ManualFixedUpdate 的 postfix，每帧无条件驱动跨越，
        // 不依赖大脑动作状态（护航动作因 IsCome 结束/意图切换时跨越驱动不再断供）
        public static void OnMoverTick(BotOwner botOwner)
        {
            if (botOwner == null)
            {
                return;
            }

            // bot 销毁后清掉记录，避免字典跨局累积（此时位置也不再有意义）
            if (botOwner.GetPlayer == null)
            {
                LastMoverPositions.Remove(botOwner);
                RescueWatches.Remove(botOwner);
                return;
            }

            // 位移侦查必须放在跨越驱动之前：驱动本身会移动 bot，放后面会把跨越的正常推进也算进去
            var position = botOwner.Position;
            if (LastMoverPositions.TryGetValue(botOwner, out var lastPosition))
            {
                // 阈值 0.6m：正常行走每帧位移上限 0.3m —— OnMotionApplied 的 num 是
                // min(deltaMove.magnitude, 0.3f)（BotMoverImpostor.cs:109-113），而 PositionOnWayInner
                // 的推进量就是它，Transform 也按它写（BotMover.cs:938）。超过 0.6m 必是写位置事件
                var jump = (position - lastPosition).magnitude;
                if (jump > 0.6f)
                {
                    CrossingBots.TryGetValue(botOwner, out var moving);
                    NavBridgeDebug.Log($"exec.snap.{botOwner.Id}", $"单帧位移 {jump:F1}m（正常 ≤0.3m）：{lastPosition.ToString("F1")} → {position.ToString("F1")}；跨越中={moving != null} hop={(moving != null && moving._hopping)} relink={RelinkInProgress}");
                }
            }

            LastMoverPositions[botOwner] = position;

            WatchRescue(botOwner, position);

            if (CrossingBots.TryGetValue(botOwner, out var executor))
            {
                executor.Tick(botOwner);
            }
        }

        // "离开 navmesh 且长时间不动"的自救（详见 RescueWatches 的说明）。
        // 只在执行器没接管这个 bot 时生效：翻越过程中 bot 本来就会短暂离开网格（动画把身体抬起来、
        // 落点还在障碍另一侧），那一段由 _hopping / FinishAndRelink 负责，不能被这里插手。
        // 反过来，站在网格上原地待命（等玩家）不会被误伤——那时采样每次都成功、锚点一直在被重置
        private static void WatchRescue(BotOwner botOwner, Vector3 position)
        {
            if (!RescueWatches.TryGetValue(botOwner, out var watch))
            {
                watch = new RescueWatch();
                RescueWatches[botOwner] = watch;
            }

            var now = Time.time;

            // navmesh 采样按 0.5s 节流：OnMoverTick 是每帧每个 bot 都要走的
            if (now >= watch.NextProbeTime)
            {
                watch.NextProbeTime = now + 0.5f;
                if (NavMesh.SamplePosition(position, out var sample, 0.5f, -1))
                {
                    watch.LastGoodPos = sample.position;
                    watch.HasLastGood = true;
                    watch.Anchor = position;
                    watch.AnchorTime = now;
                    return;
                }
            }

            if (CrossingBots.ContainsKey(botOwner) || !watch.HasLastGood)
            {
                return;
            }

            if (position.McsSqrDistance(watch.Anchor) > 0.5f * 0.5f)
            {
                watch.Anchor = position;
                watch.AnchorTime = now;
                return;
            }

            if (now - watch.AnchorTime < 3f)
            {
                return;
            }

            NavBridgeDebug.Log($"exec.rescue.{botOwner.Id}", $"离开 navmesh 且 {now - watch.AnchorTime:F1}s 位移不足 0.5m（pos={position.ToString("F1")}），拉回最后一次确认可达的网格点 {watch.LastGoodPos.ToString("F1")}（相距 {Vector3.Distance(position, watch.LastGoodPos):F1}m）");

            // 锚点直接挪到目标点：救援后 bot 就在那里，不需要再等一次"没动"的判定累积
            watch.Anchor = watch.LastGoodPos;
            watch.AnchorTime = now;
            watch.NextProbeTime = now + 1f;
            RescueInProgress = true;
            try
            {
                botOwner.Mover.SetPlayerToNavMesh(watch.LastGoodPos);
            }
            finally
            {
                RescueInProgress = false;
            }
        }

        public void Begin(NavGapInfo gap, BotOwner botOwner)
        {
            // 【2026-09-12 23:53 日志定死】同一个障碍被反复重进 Begin 时，失败额度必须带过来。
            // 循环链条：NavBridge.TryPlanStepDown 只由 IsCrossing 挡着；而 ShouldAbort 在
            // 「目标距站立点 >20m」时每秒 Cancel 一次（NavBridge.cs:68），Cancel 把 IsCrossing 置假、
            // 紧接着同一个 CanGetPathToRun 又往下走 TryPlanStepDown（McsBaseLayer.cs:1848→1877）
            // 用它自己重规划把 IsCrossing 置回真 —— 于是每秒一次 Begin，_vaultTryCount 每秒被清零。
            // 实测 23:53:17.530~23:53:30.936 同一个 typhoon_pas_col 连续 10 次 TryVaulting 全是
            // 「第 1 次失败」（站位/落点逐字不变，目标点在 90m 外、绕路 114m），bot 被一直按在障碍上，
            // 15s 后由游戏自己的 ForceTeleportCommandAction 把它传走 111.9m —— 用户看到的瞬移就是这个。
            // 站立点相距 1m 内且是同一个 bot 即视为同一障碍：把额度带过来，让「两次失败即拉黑 60s」
            // 真正落地；换了障碍则照旧清零
            var sameGap = _hasGap && _botOwner == botOwner
                && Vector3.Distance(gap.NearPoint, _lastGapNear) <= 1f;
            if (sameGap)
            {
                NavBridgeDebug.Log("vault.exec.replan", $"同一障碍重新规划：站立点 {gap.NearPoint.ToString("F1")} 距上次站立点 {Vector3.Distance(gap.NearPoint, _lastGapNear):F2}m，失败额度 {_vaultTryCount} 次带到本次（不清零，否则「两次失败即拉黑」永不生效）");
            }

            _lastGapNear = gap.NearPoint;
            _hasGap = true;

            _edge = gap.NearPoint;
            _destination = gap.FarPoint;
            _type = gap.Type;
            _botOwner = botOwner;
            _startTime = Time.time;
            _hopping = false;
            _arrived = false;
            if (!sameGap)
            {
                _vaultTryCount = 0;
            }

            _nextVaultTryTime = 0f;
            _pressAnchor = botOwner.Position;
            _pressAnchorTime = Time.time;
            _arrivedTime = 0f;
            _nextPressTime = 0f;
            _pushingIntoObstacle = false;
            _bestApproachDistance = float.MaxValue;
            _nextStallLogTime = 0f;
            IsCrossing = true;
            CrossingBots[botOwner] = this;
            NavBridgeDebug.Log("exec.begin", $"开始跨越：type={_type} edge={_edge.ToString("F1")} dest={_destination.ToString("F1")} way角点={gap.Way.Length}");
        }

        private void Tick(BotOwner botOwner)
        {
            // 单帧位移侦查已上移到 OnMoverTick：那里对每个 bot 无条件执行，跨越被 Cancel
            // （接近失败/超时/自检放弃）之后仍能继续抓到写位置事件
            var pos = botOwner.Position;

            if (_type == ENavGapType.Vault)
            {
                TickVault(botOwner, pos);
                return;
            }

            if (ShouldFinish(botOwner))
            {
                FinishAndRelink(botOwner);
                return;
            }

            var botPosition = botOwner.Position;
            var toEdge = _edge - botPosition;
            toEdge.y = 0f;

            if (!_hopping)
            {
                botOwner.GoToSomePointData.SetPoint(_edge);

                var toEdgeNow = _edge - botPosition;
                toEdgeNow.y = 0f;
                var isCome = botOwner.GoToSomePointData.IsCome();
                //NavBridgeDebug.Log("exec.approach", $"接近边缘：pos={botPosition.ToString("F1")} toEdge={toEdgeNow.magnitude:F2} IsCome={isCome}");

                // 到达边缘（含 EFT 自身停下的情况）即进入 hop
                if (toEdgeNow.magnitude <= 2.5f || isCome)
                {
                    //NavBridgeDebug.Log("exec.hop", $"进入 hop（距边缘 {toEdgeNow.magnitude:F2}m，IsCome={isCome}），恢复原生行走跨过边缘");
                    _hopping = true;
                    botOwner.GoToSomePointData.Point = _destination;
                }
                return;
            }

            // hop：恢复原生行走——无路径直走落点，由 MovePlayer 驱动；
            // 边缘处障碍检测/链接守卫已由补丁挂起，允许 bot 走出边缘自然落下
            botOwner.Mover.GoToPointNoWay(_destination);
            botOwner.GoToSomePointData.Point = _destination;
            botOwner.GoToSomePointData._lastPosibleRecalc = Time.time;
            botOwner.GoToSomePointData._pointhRefreshed = false;
            // 高频采样：记录 y 下降曲线，用于区分真实重力下落（0.65s/2m）与瞬移（一帧完成）
            //NavBridgeDebug.Log("exec.hopping", $"hop 中：pos={botPosition.ToString("F2")} dest={_destination.ToString("F1")}", 0.15f);
        }

        // 功能①：翻越执行。接近障碍 → 面向障碍 + 停冲刺 → TryVaulting（组件自带扫描/策略）→
        // 成功则挂起链接守卫（复用功能③机制，翻越动画期间 bot 离开 navmesh 表面）→ 越过障碍面且落在网格上后 relink
        private void TickVault(BotOwner botOwner, Vector3 pos)
        {
            if (_hopping)
            {
                // 翻越动画进行中：越过障碍面（沿 Near→Far 方向推进过 60%）且落在网格上才算完成
                if (IsAcrossObstacle(pos, out _))
                {
                    //NavBridgeDebug.Log("vault.exec.finish", $"翻越完成 pos={pos.ToString("F1")}");
                    FinishAndRelink(botOwner);
                    return;
                }

                // 攀爬把护航抬到障碍顶上（护栏、檐口）而顶上没有 navmesh：动画早已结束，bot 却既不前进
                // 也踩不到网格。一直等满 12s 只会让它杵在上面，最后还要靠 EFT 的救援传送才下得来。
                // 2.5s 无位移 + 脚下 0.5m 内没有网格即判定"落在障碍顶上"：拉黑障碍并交回大脑，
                // 下一帧功能③（边缘走下）就会把 bot 送下去
                if (Time.time - _hopStartTime > 2.5f && IsPressed(pos) && !NavMesh.SamplePosition(pos, out _, 0.5f, -1))
                {
                    NavBridgeDebug.Log("vault.exec.stuckOnTop", $"翻越停滞：动画开始后 {Time.time - _hopStartTime:F1}s 无位移且脚下 0.5m 内无 navmesh（pos={pos.ToString("F1")}，落点={_destination.ToString("F1")}），判定落在障碍顶上，拉黑障碍并交回大脑走下");
                    BlacklistVault(_edge, true);
                    Cancel();
                    return;
                }

                if (Time.time - _startTime > 12f)
                {
                    //NavBridgeDebug.Log("vault.exec.timeout", "翻越超时 12s，拉黑障碍放弃");
                    BlacklistVault(_edge, true);
                    Cancel();
                }
                return;
            }

            // 翻越未必由我们这次 TryVaulting 完成：自动翻越开启时 VaultingComponent.DoVaultingTick 每帧
            // Tick + TryDoAutomaticVaulting，bot 顶着障碍走两步就被它自己送过去了，而执行器还停在接近/尝试阶段、
            // _edge 仍在障碍近侧 ⇒ 推进目标会把 bot 从对面硬拉回障碍前面；往回走时移动方向与 faceDir 相反，
            // 紧接着又会撞上"朝向自检未过"（gridOff 取的是上一帧骨架朝向，≈180°）→ 放弃 → 冷却后重规划 → 无限来回。
            // 所以每帧先判"是不是已经在对面"：在就按完成收尾，任何阶段都不再把 bot 往回拉
            if (IsAcrossObstacle(pos, out var acrossProgress))
            {
                NavBridgeDebug.Log("vault.exec.alreadyAcross", $"翻越已由其它途径完成：已推进 {acrossProgress:F2}m（站立点→落点全程的 60% 即判定跨过），pos={pos.ToString("F1")} 站立点={_edge.ToString("F1")} 落点={_destination.ToString("F1")}；按完成收尾，不再回拉");
                FinishAndRelink(botOwner);
                return;
            }

            // 接近阶段：先正常寻路走到障碍附近，最后一段改成直线"顶"上去
            if (!_arrived)
            {
                var toEdge = _edge - pos;
                toEdge.y = 0f;

                // 到位判据必须用组件实测的障碍距，不能用 IsCome()，也不能只满足于"走到站立点"：
                // 实测 IsCome() 在距站立点 1.48m 就置真（bot 自身的到达阈值是 FileSettings.Move.REACH_DIST
                // ≈1.5m），而 EFT 只在距障碍 ≤ MinDistantToInteract(0.5m) 时才给翻越策略
                // （VaultMoveModel.CheckVaultCondition 的 flag3 = MinDistantToInteract >= DistanceToMainObstacle）。
                // 差这 1m 就足以让整条链路死在 flag3 上，七道门里其余六道连算都没算过
                var component = botOwner.GetPlayer.VaultingComponent as VaultingComponent;
                component?.Tick();
                var hasObstacle = VaultGateProbe.TryMeasureObstacle(component, out var obstacleDistance);

                // 站立点是 navmesh 样点，而 navmesh 按 agent 半径烘、铺不到贴着障碍的地方，走到站立点也未必够近；
                // GoToPointNoWay 的单点路径自身可接受距离也只有 0.5m（AbstractBotPath.ReachDist），照样会提前停下。
                // 所以最后 2.5m 把目标点放到障碍前方 1m 处、靠直线推进顶上去：bot 会被障碍的碰撞挡住，
                // 中心停在距障碍面约一个胶囊半径（0.35m）的地方，正落在 MinDistantToInteract 之内。
                // 直线推进不绕行，但前提是已经离得很近（2.5m 内），更远时仍交给寻路避免撞墙角。
                // 注意这里绝不挂起 SetPlayerToNavMesh：顶住阶段之所以不需要冻结，是因为 OnMotionApplied 的
                // 推进量取自实际位移 deltaMove，被障碍挡住时 deltaMove → 0、PositionOnWay 自动停住，
                // 不可能越过障碍跑到对面去采样（详见文件上方 IsLinkWriteFrozen 删除处的原因说明）
                if (toEdge.magnitude <= 2.5f)
                {
                    _pushingIntoObstacle = true;
                    PressInto(botOwner);
                }
                else
                {
                    _pushingIntoObstacle = false;
                    botOwner.GoToSomePointData.SetPoint(_edge);
                }

                NavBridgeDebug.Log("vault.exec.approach", $"接近障碍：pos={pos.ToString("F1")} 站立点={_edge.ToString("F1")} 距 {toEdge.magnitude:F2}m 组件实测障碍距={(hasObstacle ? obstacleDistance.ToString("F2") : "无")} IsMoving={botOwner.Mover.IsMoving}", 0.5f);

                // 卡住取证：距离不再缩短时把 EFT 侧与"移动记账"有关的状态全打出来。
                // 只打"距多少米"分不清"被障碍挡住"和"移动记账被冻住"——两者的位置表现完全一样
                if (toEdge.magnitude < _bestApproachDistance - 0.05f)
                {
                    _bestApproachDistance = toEdge.magnitude;
                    // 必须往后推 1s 而不是置 0：置 0 会让"距离还在缩短"的下一帧立刻打出一条假的
                    // vault.exec.stall（实测 23:06:31.865 —— exec.begin 之后 0.012s 就报"接近受阻"，
                    // 而同一段 32.485 的 approach 明明在缩短 1.78 → 1.40）
                    _nextStallLogTime = Time.time + 1f;
                }
                else if (Time.time >= _nextStallLogTime)
                {
                    _nextStallLogTime = Time.time + 1f;
                    NavBridgeDebug.Log("vault.exec.stall", DescribeStall(botOwner, pos, toEdge.magnitude, hasObstacle, obstacleDistance));
                }

                if (hasObstacle && obstacleDistance <= 0.45f)
                {
                    MarkArrived($"组件实测障碍距 {obstacleDistance:F2}m ≤ 0.45m（距站立点 {toEdge.magnitude:F2}m）");
                    return;
                }

                // 顶住判据：已经贴到站立点跟前却 0.4s 不再前进 —— 被障碍挡住了。
                // 走到这一步说明组件也没量出更近的障碍（薄围栏可能正好落在 0.1m 网格步长的缝里），
                // 但物理上 bot 已经顶在障碍上，此时试一试仍是唯一机会
                if (toEdge.magnitude <= 1.2f && IsPressed(pos))
                {
                    MarkArrived($"顶住障碍：距站立点 {toEdge.magnitude:F2}m 且 0.4s 无位移（组件实测障碍距={(hasObstacle ? obstacleDistance.ToString("F2") : "无")}）");
                    return;
                }

                // 走不到站立点就别耗着：早前站立点落在障碍内部，bot 会一直钉在原地、
                // 既不移动也永远到不了，从此再无任何翻越尝试（也不拉黑——到不了是可达性问题，
                // 不是"这个障碍翻不过去"，冷却后还会重新规划）
                if (Time.time - _startTime > 8f)
                {
                    NavBridgeDebug.Log("vault.exec.approachTimeout", $"接近障碍超时 8s：pos={pos.ToString("F1")} 站立点={_edge.ToString("F1")} 距 {toEdge.magnitude:F2}m，放弃本次翻越");
                    NextPlanCooldownUntil = Time.time + 8f;
                    Cancel();
                }
                return;
            }

            // 翻越尝试阶段：面向障碍（扫描沿身体 yaw 朝向）+ 停冲刺（冲刺禁用 Vault/Climb 策略）
            var faceDir = _destination - _edge;
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude > 0.001f)
            {
                faceDir.Normalize();
                botOwner.Steering.LookToPoint(_edge + faceDir * 2f);
            }

            if (botOwner.GetPlayer.MovementContext.IsSprintEnabled)
            {
                botOwner.Mover.Sprint(false);
            }

            var vaultingComponent = botOwner.GetPlayer.VaultingComponent as VaultingComponent;

            // 等待期间继续顶着障碍：松开推进会带一点惯性回退，第二次尝试时就量不到 ≤0.5m 了
            if (Time.time < _nextVaultTryTime)
            {
                PressInto(botOwner);
                return;
            }

            // 【2026-09-12 22:36 日志定死】策略门"anim过渡"的等待。GetVaultingStrategy() 的头两道门是
            //   if (IsAnimatorInTransitionState(0) || PlayerAnimatorIsJumpSetted()) return None;
            // （VaultingComponent.cs:200），而 TryVaulting() 只把策略结果交给 DoVaultingByStrategy（:107）
            // —— 于是会出现"CanVaulting=True、Vault 与 Climb 的 CanMove() 全为 True、strategy 却是 None"。
            // 实测 22:36:00.977 那次就是这样：站位距边缘 0.08m、距=0.40m、高/长/背高比/顶净空全达标，
            // 唯独 anim过渡×，TryVaulting=False 白占一次失败额度；0.43s 后 EFT 的 navmesh 救援把 bot
            // 一帧送到对面，alreadyAcross 才把这次穿墙当成"翻越已完成"收尾。
            // 过渡几乎是我们自己造成的：顶住阶段一路在冲刺（stall 日志里 冲刺=True），到场后才
            // Mover.Sprint(false)（紧邻上方），而到位等待只有 0.2s —— 冲刺→行走的过渡还没走完。
            // 过渡期不代表"翻不过去"，等它结束再试，且不计入 _vaultTryCount。
            // 上限 2.5s：动画若因异常卡在过渡态，不能无限等，到点照常尝试（把真实原因交给下面的 Describe）
            if (VaultGateProbe.IsAnimatorBusy(vaultingComponent) && Time.time - _arrivedTime < 2.5f)
            {
                NavBridgeDebug.Log("vault.exec.animWait", $"动画过渡中（anim过渡/跳跃标记），等它走完再试：到位后已等 {Time.time - _arrivedTime:F1}s", 0.5f);
                PressInto(botOwner);
                return;
            }

            // LookToPoint 只写转向目标、由 Steering() 按速率逐帧推进，且会被护航跟随逻辑每帧的
            // LookToMovingDirection 顶回移动方向（GoToPointLogic.cs:29）→ 到点直接写正身体 yaw；
            // 再补一次 Tick，避免自动翻越开启时复用上一帧的旧扫描结果（详见 VaultAim）
            var aimAngle = VaultAim.FaceYaw(botOwner, faceDir);
            vaultingComponent?.Tick();

            // 朝向自检（详见 VaultAim.TryAlign）：写正 yaw 不等于网格就扫在障碍上。超限且重掰无效时
            // 立刻结束本次翻越 —— 这种尝试注定扫不到障碍（退化成 "terrain=true、长=2.00"），
            // 只会白占一次失败额度。这里不拉黑障碍，与接近超时同样只加规划冷却，避免立刻重规划同一缺口形成死循环
            if (!VaultAim.TryAlign(botOwner, vaultingComponent, faceDir, out var aimDetail))
            {
                // 越过量一起打：正值且接近全程说明 bot 其实已经在障碍另一侧（不该再掰朝向、更不该往回走），
                // 那是"翻越已由自动翻越完成但执行器不知道"的形态，正常应由上面的 alreadyAcross 兜住
                NavBridgeDebug.Log("vault.exec.aim.giveup", $"{aimDetail}；越过量 {Vector3.Dot(pos - _edge, faceDir):F2}m（站立点→落点为全程）；放弃本次翻越，改走绕路");
                NextPlanCooldownUntil = Time.time + 8f;
                Cancel();
                return;
            }

            _vaultTryCount++;
            var tryResult = botOwner.GetPlayer.VaultingComponent.TryVaulting();
            if (tryResult)
            {
                NavBridgeDebug.Log("vault.exec.try", $"第 {_vaultTryCount} 次 TryVaulting = True（朝向修正 {aimAngle:F0}°，站位距边缘 {Vector3.Distance(botOwner.Position, _edge):F2}m；{aimDetail}）");
                // 先撤掉顶上去时留下的单点路径：翻越过程由动画的 root motion 驱动，
                // 留着 mover 的路径会让它一路推到"站立点+1m"就在半途停下，卡到 12s 超时被拉黑
                botOwner.Mover.Stop();
                botOwner.GetPlayer.OnVaulting();
                _hopping = true;
                _hopStartTime = Time.time;
                return;
            }

            // false 只说明七道门里有不达标的，把每道门的两侧量都打出来（VaultGateProbe）
            NavBridgeDebug.Log("vault.exec.try", $"第 {_vaultTryCount} 次 TryVaulting = False（朝向修正 {aimAngle:F0}°，站位距边缘 {Vector3.Distance(botOwner.Position, _edge):F2}m；{aimDetail}）；{VaultGateProbe.Describe(vaultingComponent)}");

            if (_vaultTryCount >= 2)
            {
                // 两次尝试失败：拉黑该障碍（60s），Cancel 后寻路自然给出绕路结果
                BlacklistVault(_edge, true);
                Cancel();
            }
            else
            {
                _nextVaultTryTime = Time.time + 0.8f;
            }
        }

        // 到位：停止推进，留 0.2s 让 bot 把顶住障碍时的惯性滑完再扫瞄。
        // 时长比原先的 0.6s 短——此时 bot 已经贴在障碍上，多在原地等只会让跟随/避障逻辑
        // 有机会把它推离障碍，反而把刚量到的 ≤0.45m 破坏掉
        private void MarkArrived(string reason)
        {
            _arrived = true;
            _arrivedTime = Time.time;
            _nextVaultTryTime = Time.time + 0.2f;
            NavBridgeDebug.Log("vault.exec.arrive", $"到位：{reason}");
        }

        // 顶着障碍推进：0.25s 节流下发单点路径（每帧重建会一直重置 mover 的路径进度）。
        // 【2026-09-12 22:19 日志定死】必须同步写 GoToSomePointData.Point：只写 Mover.GoToPointNoWay 时
        // Point 会一直烂在上一次的站立点上——实测 22:19:16.073 与 22:19:48.718 两次都打出
        // "目标点=(-772.0,-59.6,467.3) 到目标 0.19m"，而大脑正是拿这个 Point 判 IsCome()
        // （BotGoToPointData.cs:87，阈值 REACH_DIST≈1.5m）。0.19m 远小于 1.5m ⇒ 护航的移动动作
        // 认定"已经到位"而收工，bot 在距站立点 1.66m / 2.21m 处各自钉死 8 秒，最后 approachTimeout
        private void PressInto(BotOwner botOwner)
        {
            if (Time.time < _nextPressTime)
            {
                return;
            }

            _nextPressTime = Time.time + 0.25f;
            var pressTarget = PressTarget();
            botOwner.GoToSomePointData.SetPoint(pressTarget);
            botOwner.Mover.GoToPointNoWay(pressTarget);
        }

        // 接近阶段的卡住取证。这条链很深：SetPlayerToNavMesh 既驱动每帧位移、又更新 PrevSuccessLinkedFrom →
        // method_12 拿它判是否把 bot 拽回锚点（>0.3m 就拽）→ MovePlayer 用 PositionOnWayInner 算方向与角点进度。
        // "被障碍挡住"和"被 method_12 拽回"在位置表现上完全一样（都是原地抖动），所以一次把各环节的值都打出来：
        // 判据是"两者相距"——持续接近 0.30m 上限 ⇒ 是 method_12 在拽；远小于 0.30m 且 deltaMove 归零 ⇒ 真的被挡住
        private string DescribeStall(BotOwner botOwner, Vector3 pos, float toEdge, bool hasObstacle, float obstacleDistance)
        {
            var mover = botOwner.Mover;
            var goTo = botOwner.GoToSomePointData;
            var movementContext = botOwner.GetPlayer.MovementContext;
            return $"接近受阻：pos={pos.ToString("F1")} 站立点={_edge.ToString("F1")} 距 {toEdge:F2}m 推进={(_pushingIntoObstacle ? "直线顶住" : "正常寻路")}"
                + $" 组件实测障碍距={(hasObstacle ? obstacleDistance.ToString("F2") : "无")}"
                + $" | IsMoving={mover.IsMoving} 有路径={mover.HasPathAndNoComplete} 允许传送={mover._allowTeleport} 暂停={mover.Pause} 距上次位移 {Time.time - mover._lastTimePosChanged:F1}s"
                + $" | PositionOnWay={mover.PositionOnWay.ToString("F1")} 上次链接点={mover.PrevSuccessLinkedFrom.ToString("F1")} 两者相距 {Vector3.Distance(mover.PositionOnWay, mover.PrevSuccessLinkedFrom):F2}m（>0.30m 会被 method_12 拽回）"
                + $" | 目标点={goTo.Point.ToString("F1")} 到目标 {goTo._distToPoint:F2}m 待重算已置={goTo._pointhRefreshed}"
                + $" | 身体朝向={movementContext.PlayerRealForward.ToString("F2")} 冲刺={movementContext.IsSprintEnabled}"
                + $" | 周围1.5m：{DescribeNeighbours(pos)}";
        }

        // 静态障碍探针查不到角色碰撞体：同行 bot 或玩家堵在面前时表现为"无障碍却走不动"
        private static string DescribeNeighbours(Vector3 pos)
        {
            var found = new List<string>();
            var colliders = Physics.OverlapSphere(pos + Vector3.up, 1.5f);
            foreach (var collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                found.Add($"{collider.name}({LayerMask.LayerToName(collider.gameObject.layer)})");
                if (found.Count >= 8)
                {
                    break;
                }
            }

            return found.Count == 0 ? "无" : string.Join(" / ", found);
        }

        // 顶住目标：站立点再沿"站立点→落点"方向推 1m（正好越过障碍面）。bot 会被障碍的碰撞挡住，
        // 中心停在距障碍面约一个胶囊半径（0.35m）处，正落在 MinDistantToInteract 之内
        private Vector3 PressTarget()
        {
            var dir = _destination - _edge;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
            }

            return _edge + dir;
        }

        // 顶住判据：0.4s 内累计位移 < 5cm 视为被障碍挡住。
        // 直线推进不绕行，没被挡住时 bot 每帧都在往前走（位移远超 5cm），所以这个判据很干净
        private bool IsPressed(Vector3 pos)
        {
            if (pos.McsSqrDistance(_pressAnchor) > 0.05f * 0.05f)
            {
                _pressAnchor = pos;
                _pressAnchorTime = Time.time;
                return false;
            }

            return Time.time - _pressAnchorTime > 0.4f;
        }

        // 是否已经站到障碍另一侧：沿 Near→Far 方向推进过全程 60%，且脚下确实踩在 navmesh 上。
        // 用相对量（60%）而不是绝对距离：站立点到障碍面的距离会被 navmesh 采样吸附（实测 0.51m，
        // 上限约 1.15m），而"顶着障碍"时的推进量只有 站距 − 0.35m（胶囊半径）—— 绝对阈值会把两种状态混在一起。
        // progress 回传给调用方进日志，便于事后反推几何
        private bool IsAcrossObstacle(Vector3 pos, out float progress)
        {
            progress = 0f;
            var dir = _destination - _edge;
            dir.y = 0f;
            var total = dir.magnitude;
            if (total < 0.01f)
            {
                return false;
            }

            progress = Vector3.Dot(pos - _edge, dir / total);
            return progress > total * 0.6f && NavMesh.SamplePosition(pos, out _, 0.3f, -1);
        }

        // 跨越收尾：结束状态机 + 主动重链接到脚下 navmesh。
        // relink 期间必须放行 SetPlayerToNavMesh（RelinkInProgress），否则跨越期的挂起会让它空转。
        // 完成判定一律先要求脚下 0.3m 内有网格（比 relink 内部 FindSamplePoint 的 0.4f 更严）：
        // 半空 relink 采样失败会走 superFail 分支，FindBetterPosition(100m) 取最近 navmesh
        // 可能是上层边缘，表现为"被拉回原地"
        private void FinishAndRelink(BotOwner botOwner)
        {
            Cancel();
            RelinkInProgress = true;
            try
            {
                botOwner.Mover.SetPlayerToNavMesh(botOwner.Position);
            }
            finally
            {
                RelinkInProgress = false;
            }
        }

        private bool ShouldFinish(BotOwner botOwner)
        {
            var pos = botOwner.Position;

            // 完成判定必须确认 bot 已实际踩在 navmesh 上：落点与目标之间可能存在网格空洞，
            // 半空 relink 采样失败 → superFail → Teleport 被 FindBetterPosition 拉回上层边缘（实测复现）。
            // 半径用 0.3f：必须严于 relink 内部 FindSamplePoint 的 0.4f，否则出现"校验通过但 relink 必失败"的窗口
            if (pos.McsSqrDistance(_destination) <= 1f * 1f)
            {
                if (NavMesh.SamplePosition(pos, out _, 0.3f, -1))
                {
                    //NavBridgeDebug.Log("exec.finish.reached", $"已到达落点（pos={pos.ToString("F1")}），跨越结束");
                    return true;
                }

                //NavBridgeDebug.Log("exec.finish.waitMesh", $"接近落点但下方无 navmesh，继续行走（pos={pos.ToString("F2")}）", 0.5f);
                return false;
            }

            // 已落到目标层高（水平位置不必精确，剩下的路交回正常寻路）
            if (_hopping && pos.y - _destination.y <= 0.2f)
            {
                if (NavMesh.SamplePosition(pos, out _, 0.3f, -1))
                {
                    //NavBridgeDebug.Log("exec.finish.landed", $"已落到目标层高（pos={pos.ToString("F1")}），交回正常寻路");
                    return true;
                }

                //NavBridgeDebug.Log("exec.finish.waitMesh", $"已到目标层高但下方无 navmesh，继续行走（pos={pos.ToString("F2")}）", 0.5f);
                return false;
            }

            if (Time.time - _startTime > 10f)
            {
                //NavBridgeDebug.Log("exec.finish.timeout", $"跨越超时 10s（pos={pos.ToString("F1")}），进入 8s 规划冷却");
                NextPlanCooldownUntil = Time.time + 8f;
                return true;
            }

            return false;
        }

        public void Cancel()
        {
            if (_botOwner != null)
            {
                if (_hopping)
                {
                    RecentHopEnds[_botOwner] = Time.time;
                }
                else
                {
                    // 【2026-09-12 23:06 日志定死】放弃时把顶住阶段留下的单点路径清掉：它的目标点在
                    // 障碍面之后（PressTarget = 站立点 + 前向 1m），而 PositionOnWayInner 推的是"路径
                    // 进度"、不受碰撞体阻挡 —— 路径留着，虚拟点就会继续朝障碍里走，越过障碍后
                    // TryExtraSample 的 2m 兜底采样就能一帧把 bot 送到对面。实测放弃后 0.06s 就发生。
                    // 与翻越成功时的处理一致（TickVault 里 TryVaulting = True 那条也调 Mover.Stop()）。
                    // 大脑若要继续走绕路会自己重新下发路径，这里只是撤掉我们留下的那条
                    _botOwner.Mover.Stop();
                    RecordAbort(_botOwner);
                }
            }

            IsCrossing = false;
            _hopping = false;
            _arrived = false;
            _pushingIntoObstacle = false;
            if (_botOwner != null)
            {
                CrossingBots.Remove(_botOwner);
            }
        }

        // 记录一次"未进入翻越动画的放弃"，并顺手清理过期项（字典是静态的、跨局存活）
        private static void RecordAbort(BotOwner botOwner)
        {
            var now = Time.time;
            if (RecentAborts.Count >= 8)
            {
                var stale = new List<BotOwner>();
                foreach (var pair in RecentAborts)
                {
                    if (now - pair.Value >= AbortGuardDuration)
                    {
                        stale.Add(pair.Key);
                    }
                }

                foreach (var owner in stale)
                {
                    RecentAborts.Remove(owner);
                }
            }

            RecentAborts[botOwner] = now;
        }
    }
}
