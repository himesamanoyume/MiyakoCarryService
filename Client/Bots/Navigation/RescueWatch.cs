using EFT;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Navigation
{
    public class RescueWatch
    {
        public Vector3 LastGoodPos;
        public bool HasLastGood;
        public Vector3 Anchor;
        public float AnchorTime;
        public float NextProbeTime;

        public void Update(BotOwner botOwner, Vector3 position, bool isCrossing)
        {
            var now = Time.time;
            if (now >= NextProbeTime)
            {
                NextProbeTime = now + 0.5f;
                if (NavMesh.SamplePosition(position, out var sample, 0.5f, -1))
                {
                    LastGoodPos = sample.position;
                    HasLastGood = true;
                    Anchor = position;
                    AnchorTime = now;
                    return;
                }
            }

            if (isCrossing || !HasLastGood)
            {
                return;
            }

            if (position.McsSqrDistance(Anchor) > 0.5f * 0.5f)
            {
                Anchor = position;
                AnchorTime = now;
                return;
            }

            if (now - AnchorTime < 3f)
            {
                return;
            }

            Anchor = LastGoodPos;
            AnchorTime = now;
            NextProbeTime = now + 1f;
            NavGapExecutor.RescueInProgress = true;
            try
            {
                botOwner.Mover.SetPlayerToNavMesh(LastGoodPos);
            }
            finally
            {
                NavGapExecutor.RescueInProgress = false;
            }
        }
    }
}
