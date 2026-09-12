using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Navigation
{
    public class NavGapExecutor
    {
        private static readonly Dictionary<BotOwner, NavGapExecutor> CrossingBots = new Dictionary<BotOwner, NavGapExecutor>();

        // 跨越结束后 1.5s 内的 bot（Funnel 补丁据此记录 hop 刚结束时的传送事件，诊断"半空拉回"）
        private static readonly Dictionary<BotOwner, float> RecentHopEnds = new Dictionary<BotOwner, float>();

        private Vector3 _edge;
        private Vector3 _destination;
        private BotOwner _botOwner;
        private float _startTime;
        private bool _hopping;
        private Vector3 _lastTickPos;
        private bool _hasLastTickPos;

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

        // hop 结束后 within 秒内返回 true（诊断：hop 刚结束时的传送/写位置事件值得关注）
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
            _botOwner = botOwner;
            _startTime = Time.time;
            _hopping = false;
            IsCrossing = true;
            CrossingBots[botOwner] = this;
            //NavBridgeDebug.Log("exec.begin", $"开始跨越：edge={_edge.ToString("F1")} dest={_destination.ToString("F1")} way角点={gap.Way.Length}");
        }

        private void Tick(BotOwner botOwner)
        {
            // 单帧位移侦查：原生跑速每帧 <0.1m，跨越中单帧位移 >0.5m 必为写位置事件；
            // hopping=False 说明 hop 已被取消后链接恢复（拉回），hopping=True 说明存在未知直写路径
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

            if (ShouldFinish(botOwner))
            {
                Cancel();
                // relink 结果记录 EBotLinkResult：fail/superFail 说明半空采样失败 → superFail 分支的
                // FindBetterPosition(100m) 会取最近 navmesh（可能是上层边缘）→"拉回原地"嫌疑路径一
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
