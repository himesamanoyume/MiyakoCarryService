
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal class FollowMcsBossLayer(BotOwner botOwner, int priority) : McsBaseLayer<FollowMcsBossLayer>(botOwner, priority)
    {
        public override Action GetNextAction()
        {
            var mcsBossPlayer = BotOwner.McsBossPlayer;
            if (Vector3.Distance(BotOwner.Position, mcsBossPlayer.Position) >= 25)
            {
                return new Action(typeof(FollowMcsBossLogic), "too far from the boss");
            }
            else
            {
                return new Action(typeof(McsPlayerPatrolLogic), "nothing to do");
            }
        }

        public override bool IsActive()
        {
            if (BotOwner.IsMcsPlayer) // 这会导致只要是护航 就一定会一直停留在这个Layer，需要调整
            {
                return true;
            }
            return false;
        }

        public override bool IsCurrentActionEnding()
        {
            return true;
        }
    }
}