
using Comfort.Common;
using EFT;

namespace MiyakoCarryService.Client.BotBehaviors
{
    internal class BotCarryServiceChecker : BotBehavior
    {
        public BotCarryServiceChecker(BotOwner owner) : base(owner)
        {
            
        }

        public override void ManualUpdate()
        {
            BotOwner.Memory.DeleteInfoAboutEnemy(Singleton<GameWorld>.Instance.MainPlayer);
        }
    }
}