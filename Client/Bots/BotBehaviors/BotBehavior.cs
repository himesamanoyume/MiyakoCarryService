
using EFT;

namespace MiyakoCarryService.Client.Bots.BotBehaviors
{
    public abstract class BotBehavior
    {
        public BotOwner BotOwner { get; private set; }
        public Player McsLeadPlayer { get; private set; }
        public BotBehavior(BotOwner botOwner, Player mcsLeadPlayer)
        {
            BotOwner = botOwner;
            McsLeadPlayer = mcsLeadPlayer;
        }

        public abstract void ManualUpdate();
    }
}