
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal class FollowMcsBossLayer(BotOwner botOwner, int priority) : McsBaseLayer<FollowMcsBossLayer>(botOwner, priority, botOwner.GetMcsData())
    {
        public override Action GetNextAction()
        {
            var mcsBossPlayer = McsPlayerData.BossPlayer;
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
            if (McsPlayerData.BossPlayer != null)
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