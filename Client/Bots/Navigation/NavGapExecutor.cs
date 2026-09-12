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
        private Vector3 _lastTickPos;
        private bool _hasLastTickPos;
        private bool _arrived;
        private int _vaultTryCount;
        private float _nextVaultTryTime;

        // 顶住判据用：锚点位置与其最后一次变化的时间（0.4s 无位移 ⇒ 被障碍挡住）
        private Vector3 _pressAnchor;
        private float _pressAnchorTime;

        // 直线推进目标的下发节流（每帧重建单点路径会一直重置 mover 的路径进度）
        private float _nextPressTime;

        public bool IsCrossing { get; private set; }

        // 跨越超时（大概率边缘不可跨越，如护栏/檐口）后的一段时间内不再重复规划
        public float NextPlanCooldownUntil { get; private set; }

        // 执行器落地重链接主动调用 SetPlayerToNavMesh 时置位，NavBridgeLinkWritePatch 据此放行
        public static bool RelinkInProgress;

        public bool IsHoppingNow => _hopping;

        public static bool IsHopping(BotOwner botOwner)
        {
            return botOwner != null && CrossingBots.TryGetValue(botOwner, out var executor) && executor._hopping;
        }

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
            if (botOwner != null && CrossingBots.TryGetValue(botOwner, out var executor))
            {
                executor.Tick(botOwner);
            }
        }

        public void Begin(NavGapInfo gap, BotOwner botOwner)
        {
            _edge = gap.NearPoint;
            _destination = gap.FarPoint;
            _type = gap.Type;
            _botOwner = botOwner;
            _startTime = Time.time;
            _hopping = false;
            _arrived = false;
            _vaultTryCount = 0;
            _nextVaultTryTime = 0f;
            _pressAnchor = botOwner.Position;
            _pressAnchorTime = Time.time;
            _nextPressTime = 0f;
            IsCrossing = true;
            CrossingBots[botOwner] = this;
            NavBridgeDebug.Log("exec.begin", $"开始跨越：type={_type} edge={_edge.ToString("F1")} dest={_destination.ToString("F1")} way角点={gap.Way.Length}");
        }

        private void Tick(BotOwner botOwner)
        {
            // 单帧位移侦查：原生跑速每帧 <0.1m，跨越中单帧位移 >0.5m 必为写位置事件
            var pos = botOwner.Position;
            if (_hasLastTickPos)
            {
                var jump = (pos - _lastTickPos).magnitude;
                if (jump > 0.5f)
                {
                    //NavBridgeDebug.Log("exec.snap", $"单帧位移 {jump:F2}m：{_lastTickPos.ToString("F2")} → {pos.ToString("F2")} hopping={_hopping}", 0.15f);
                }
            }
            _lastTickPos = pos;
            _hasLastTickPos = true;

            if (_type == ENavGapType.Vault)
            {
                TickVault(botOwner, pos);
                return;
            }

            if (ShouldFinish(botOwner))
            {
                Cancel();
                // relink 结果记录 EBotLinkResult：fail/superFail 说明半空采样失败 → superFail 分支的
                // FindBetterPosition(100m) 会取最近 navmesh（可能是上层边缘）→"拉回原地"路径
                RelinkInProgress = true;
                EBotLinkResult linkResult;
                try
                {
                    linkResult = botOwner.Mover.SetPlayerToNavMesh(botOwner.Position);
                }
                finally
                {
                    RelinkInProgress = false;
                }
                //NavBridgeDebug.Log("exec.relink", $"重链接到落点层，pos={botOwner.Position.ToString("F1")} result={linkResult}");
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
                var dir = _destination - _edge;
                dir.y = 0f;
                var total = dir.magnitude;
                if (total > 0.01f)
                {
                    dir.Normalize();
                    var progress = Vector3.Dot(pos - _edge, dir);
                    if (progress > total * 0.6f && NavMesh.SamplePosition(pos, out _, 0.3f, -1))
                    {
                        //NavBridgeDebug.Log("vault.exec.finish", $"翻越完成 pos={pos.ToString("F1")}");
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
                        return;
                    }
                }

                if (Time.time - _startTime > 12f)
                {
                    //NavBridgeDebug.Log("vault.exec.timeout", "翻越超时 12s，拉黑障碍放弃");
                    BlacklistVault(_edge, true);
                    Cancel();
                }
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
                // 直线推进不绕行，但前提是已经离得很近（2.5m 内），更远时仍交给寻路避免撞墙角
                if (toEdge.magnitude <= 2.5f)
                {
                    if (Time.time >= _nextPressTime)
                    {
                        _nextPressTime = Time.time + 0.25f;
                        var pressDir = _destination - _edge;
                        pressDir.y = 0f;
                        if (pressDir.sqrMagnitude > 0.001f)
                        {
                            pressDir.Normalize();
                        }

                        botOwner.Mover.GoToPointNoWay(_edge + pressDir);
                    }
                }
                else
                {
                    botOwner.GoToSomePointData.SetPoint(_edge);
                }

                NavBridgeDebug.Log("vault.exec.approach", $"接近障碍：pos={pos.ToString("F1")} 站立点={_edge.ToString("F1")} 距 {toEdge.magnitude:F2}m 组件实测障碍距={(hasObstacle ? obstacleDistance.ToString("F2") : "无")} IsMoving={botOwner.Mover.IsMoving}", 0.5f);

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

            if (Time.time < _nextVaultTryTime)
            {
                // 等待期间继续顶着障碍：松开推进会带一点惯性回退，第二次尝试时就量不到 ≤0.5m 了
                if (Time.time >= _nextPressTime)
                {
                    _nextPressTime = Time.time + 0.25f;
                    botOwner.Mover.GoToPointNoWay(_edge + faceDir);
                }
                return;
            }

            // LookToPoint 只写转向目标、由 Steering() 按速率逐帧推进，且会被护航跟随逻辑每帧的
            // LookToMovingDirection 顶回移动方向（GoToPointLogic.cs:29）→ 到点直接写正身体 yaw；
            // 再补一次 Tick，避免自动翻越开启时复用上一帧的旧扫描结果（详见 VaultAim）
            var aimAngle = VaultAim.FaceYaw(botOwner, faceDir);
            var vaultingComponent = botOwner.GetPlayer.VaultingComponent as VaultingComponent;
            vaultingComponent?.Tick();

            // 朝向自检（详见 VaultAim.TryAlign）：写正 yaw 不等于网格就扫在障碍上。超限且重掰无效时
            // 立刻结束本次翻越 —— 这种尝试注定扫不到障碍（退化成 "terrain=true、长=2.00"），
            // 只会白占一次失败额度。这里不拉黑障碍，与接近超时同样只加规划冷却，避免立刻重规划同一缺口形成死循环
            if (!VaultAim.TryAlign(botOwner, vaultingComponent, faceDir, out var aimDetail))
            {
                NavBridgeDebug.Log("vault.exec.aim.giveup", $"{aimDetail}；放弃本次翻越，改走绕路");
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
            _nextVaultTryTime = Time.time + 0.2f;
            NavBridgeDebug.Log("vault.exec.arrive", $"到位：{reason}");
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
            if (_hopping && _botOwner != null)
            {
                RecentHopEnds[_botOwner] = Time.time;
            }

            IsCrossing = false;
            _hopping = false;
            if (_botOwner != null)
            {
                CrossingBots.Remove(_botOwner);
            }
        }
    }
}
