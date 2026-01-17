
using EFT;

namespace MiyakoCarryService.Client.Bots.BotBehaviors
{
    internal abstract class BotBehavior
    {
        public BotOwner BotOwner { get; private set; }
        public Player McsBossPlayer { get; private set; }
        public BotBehavior(BotOwner botOwner, Player mcsBossPlayer)
        {
            BotOwner = botOwner;
            McsBossPlayer = mcsBossPlayer;
        }

        public abstract void ManualUpdate();
    }
}