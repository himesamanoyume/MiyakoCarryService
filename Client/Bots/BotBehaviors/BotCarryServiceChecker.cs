
using EFT;

namespace MiyakoCarryService.Client.Bots.BotBehaviors
{
    internal class BotCarryServiceChecker : BotBehavior
    {
        public BotCarryServiceChecker(BotOwner botOwner, Player McsLeadPlayer) : base(botOwner, McsLeadPlayer)
        {
            
        }

        public override void ManualUpdate()
        {
            if (BotOwner.Memory.GoalEnemy == null)
            {
                return;
            }

            // BotOwner.Memory.DeleteInfoAboutEnemy(McsLeadPlayer);

            if (!BotOwner.Memory.GoalEnemy.Person.HealthController.IsAlive)
            {
                BotOwner.Memory.GoalTarget.Clear();
                BotOwner.Memory.GoalEnemy = null;
            }
        }
    }
}