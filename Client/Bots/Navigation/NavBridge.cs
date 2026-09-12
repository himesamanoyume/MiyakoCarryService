using EFT;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Navigation
{
    public class NavBridge
    {
        private readonly NavGapExecutor _executor = new NavGapExecutor();
        private NavGapInfo _gap;

        public bool IsCrossing => _executor.IsCrossing;
        public Vector3[] Way => _gap?.Way;

        // 功能③/①：跨越规划。断开型（PathPartial）、无路可走（PathInvalid）、连通但绕路（PathComplete）都会尝试规划；
        // 检测器先试翻越（窗户/围栏，更近）再试走下（低台阶），类型均可
        public bool TryPlanStepDown(BotOwner botOwner, Vector3 targetPos, NavMeshPath path)
        {
            if (IsCrossing)
            {
                return false;
            }

            if (Time.time < _executor.NextPlanCooldownUntil)
            {
                //NavBridgeDebug.Log("plan.cooldown", "规划冷却中（上次跨越超时，疑似边缘不可跨越）");
                return false;
            }

            if (!NavGapDetector.TryDetectGap(botOwner.Position, targetPos, path, out var gap)
                || (gap.Type != ENavGapType.StepDown && gap.Type != ENavGapType.Vault))
            {
                return false;
            }

            _gap = gap;
            _executor.Begin(gap, botOwner);
            return true;
        }

        // 接近阶段仅当目标显著变化时取消（其它目标流的小幅晃动不影响跨越）；
        // hop 阶段已提交，只由执行器的到达/超时结束
        public bool ShouldAbort(Vector3 targetPos)
        {
            if (_gap == null)
            {
                return true;
            }

            if (_executor.IsHoppingNow)
            {
                return false;
            }

            var edge = _gap.NearPoint;
            var toTarget = targetPos - edge;
            toTarget.y = 0f;

            // 翻越（同层）：没有"下层"概念，仅目标远离时取消
            if (_gap.Type == ENavGapType.Vault)
            {
                if (toTarget.magnitude > 20f)
                {
                    //NavBridgeDebug.Log("plan.abort.far", $"取消翻越：目标远离 {toTarget.magnitude:F1}m");
                    return true;
                }

                return false;
            }

            // 走下（下层）：目标须保持在边缘下层区域内
            if (targetPos.y > edge.y - 0.25f)
            {
                //NavBridgeDebug.Log("plan.abort.below", $"取消跨越：目标已不在边缘下层（edge.y={edge.y:F2} target.y={targetPos.y:F2}）");
                return true;
            }

            if (toTarget.magnitude > 20f)
            {
                //NavBridgeDebug.Log("plan.abort.far", $"取消跨越：目标远离下层区域 {toTarget.magnitude:F1}m");
                return true;
            }

            return false;
        }

        public void Cancel()
        {
            _executor.Cancel();
            _gap = null;
        }
    }
}
