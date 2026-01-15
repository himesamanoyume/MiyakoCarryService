
using EFT;

namespace MiyakoCarryService.Client.BotBehaviors
{
    internal abstract class BotBehavior
    {
        public BotOwner BotOwner;
        public BotBehavior(BotOwner botOwner)
        {
            BotOwner = botOwner;
        }

        public abstract void ManualUpdate();
    }
}