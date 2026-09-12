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

        // 功能③：低台阶走下。断开型（PathPartial）、无路可走（PathInvalid）、连通但绕路（PathComplete）都会尝试规划
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

            if (!NavGapDetector.TryDetectGap(botOwner.Position, targetPos, path, out var gap) || gap.Type != ENavGapType.StepDown)
            {
                return false;
            }

            _gap = gap;
            _executor.Begin(gap, botOwner);
            return true;
        }

        // 取消条件：hop 阶段已提交，只由执行器的到达/超时/连通恢复结束；
        // 接近阶段仅当"目标不再低于边缘层"或远离下层区域时取消（其它目标流在边缘层/上层晃动不影响跨越）
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
            if (targetPos.y > edge.y - 0.25f)
            {
                //NavBridgeDebug.Log("plan.abort.below", $"取消跨越：目标已不在边缘下层（edge.y={edge.y:F2} target.y={targetPos.y:F2}）");
                return true;
            }

            var toTarget = targetPos - edge;
            toTarget.y = 0f;
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
