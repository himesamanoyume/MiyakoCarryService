
using System;
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
            try
            {
                if (McsBotPlayerData == null)
                {
                    return new Action(typeof(SimplePatrolLogic), "Mcs:leadPosNull");
                }

                if (McsBotPlayerData.EscortPos.HasValue)
                {
                    BotOwner.GoToSomePointData.SetPoint(McsBotPlayerData.EscortPos.Value);
                    BotOwner.GoToSomePointData.UpdateToGo(true);
                    return new Action(typeof(EscortToPointByWayLogic), "Mcs:EscortToPoint");
                }
                else
                {
                    return new Action(typeof(SimplePatrolLogic), "Mcs:CannotFindEscortPos");
                }
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return new Action(typeof(SimplePatrolLogic), "Mcs:Exception");
            }
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

            if (McsBotPlayerData.HasDecision(EDecision.ShouldEscort) && McsBotPlayerData.EscortPos.HasValue)
            {
                return true;
            }

            return false;
        }
    }
}