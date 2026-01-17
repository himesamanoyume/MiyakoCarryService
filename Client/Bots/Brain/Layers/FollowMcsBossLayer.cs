using System;
using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal class FollowMcsBossLayer(BotOwner botOwner, int priority) : McsBaseLayer<FollowMcsBossLayer>(botOwner, priority)
    {
        public override Action GetNextAction()
        {
            throw new NotImplementedException();
        }

        public override bool IsActive()
        {
            throw new NotImplementedException();
        }

        public override bool IsCurrentActionEnding()
        {
            throw new NotImplementedException();
        }
    }
}