using EFT;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
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

        public bool TryPlanStepDown(BotOwner botOwner, Vector3 targetPos, NavMeshPath path)
        {
            if (IsCrossing)
            {
                return false;
            }

            if (Time.time < _executor.NextPlanCooldownUntil)
            {
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

            return _gap.Type == ENavGapType.Vault ? toTarget.magnitude > 20f : (targetPos.y > edge.y - 0.25f || toTarget.magnitude > 20f);
        }

        public void Cancel()
        {
            _executor.Cancel();
            _gap = null;
        }
    }
}