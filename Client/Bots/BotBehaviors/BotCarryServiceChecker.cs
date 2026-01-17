
using EFT;

namespace MiyakoCarryService.Client.Bots.BotBehaviors
{
    internal class BotCarryServiceChecker : BotBehavior
    {
        public BotCarryServiceChecker(BotOwner botOwner, Player mcsBossPlayer) : base(botOwner, mcsBossPlayer)
        {
            
        }

        public override void ManualUpdate()
        {
            BotOwner.Memory.DeleteInfoAboutEnemy(McsBossPlayer);
        }
    }
}