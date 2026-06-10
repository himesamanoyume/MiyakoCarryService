
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Enums;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsEscortLayer : McsBaseLayer<McsEscortLayer>
    {
        public McsEscortLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            
        }

        public override void Start()
        {
            base.Start();
            if (McsBotPlayerData != null)
            {
                McsBotPlayerData.IsLooting = false;
            }
        }

        public override Action GetNextAction()
        {
            if (true)
            {
                return new Action(typeof(GoToPointLogic), "Mcs:EscortToPos");
            }
            return new Action(typeof(HoldPositionLogic), "Mcs:WaitForLead");
        }

        public override bool IsActive()
        {
            if (!IsMcsBotPlayer)
            {
                return false;
            }

            if (McsBotPlayerData == null)
            {
                return false;
            }

            if (BotOwner.Memory.IsUnderFire)
            {
                return false;
            }

            if (!McsBotPlayerData.LeadPlayer.HealthController.IsAlive)
            {
                return false;
            }

            if (McsBotPlayerData.HasDecision(EDecision.ShouldEscort))
            {
                return true;
            }

            return false;
        }
    }
}