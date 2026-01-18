
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal class FollowMcsBossLayer : McsBaseLayer<FollowMcsBossLayer>
    {
        public FollowMcsBossLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            
        }
        
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
            if (IsMcsPlayer)
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