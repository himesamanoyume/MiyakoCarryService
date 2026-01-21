
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal class McsBotPlayerCommonLayer : McsBotPlayerBaseLayer<McsBotPlayerCommonLayer>
    {
        public McsBotPlayerCommonLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {

        }

        public override Action GetNextAction()
        {
            return new Action(typeof(McsBotPlayerPatrolLogic), "nothing to do");
        }

        public override bool IsActive()
        {
            BotOwner.PriorityAxeTarget.FindTarget();
            if (BotOwner.Memory.HaveEnemy || BotOwner.Memory.IsUnderFire)
            {
                return false;
            }

            if (BotOwner.BotFollower.HaveBoss && IsMcsBotPlayer)
            {
                var mcsBossPlayer = McsBotPlayerData.BossPlayer;
                if (Vector3.Distance(BotOwner.Position, mcsBossPlayer.Position) >= 25)
                {
                    return true;
                }
            }
            return false;
        }

        public override bool IsCurrentActionEnding()
        {
            return true;
        }
    }
}