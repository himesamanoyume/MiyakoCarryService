using EFT;

namespace MiyakoCarryService.Client.Bots.BotBehaviors
{
    public class BotCarryServiceChecker : BotBehavior
    {
        public BotCarryServiceChecker(BotOwner botOwner, Player McsLeadPlayer) : base(botOwner, McsLeadPlayer)
        {
            
        }

        public override void ManualUpdate()
        {
            BotOwner.Memory.DeleteInfoAboutEnemy(McsLeadPlayer);
        }
    }
}