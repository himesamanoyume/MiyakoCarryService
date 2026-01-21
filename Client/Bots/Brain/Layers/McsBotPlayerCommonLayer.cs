
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal class McsBotPlayerCommonLayer : McsBotPlayerBaseLayer<McsBotPlayerCommonLayer>
    {
        public McsBotPlayerCommonLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {

        }

        public override Action GetNextAction()
        {
            // var mcsBossPlayer = McsPlayerData.BossPlayer;
            // if (Vector3.Distance(BotOwner.Position, mcsBossPlayer.Position) >= 25)
            // {
            //     return new Action(typeof(FollowMcsBossLogic), "too far from the boss");
            // }
            // else
            // {
            //     return new Action(typeof(McsPlayerPatrolLogic), "nothing to do");
            // }
            return new Action(typeof(McsBotPlayerPatrolLogic), "nothing to do");
        }

        public override bool IsActive()
        {
            if (IsMcsBotPlayer)
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