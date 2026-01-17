using System;
using EFT;
using MiyakoCarryService.Client.Extensions;

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
            if (BotOwner.IsMcsPlayer)
            {
                return true;
            }
            return false;
        }

        public override bool IsCurrentActionEnding()
        {
            throw new NotImplementedException();
        }
    }
}