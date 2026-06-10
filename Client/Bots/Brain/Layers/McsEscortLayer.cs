
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;

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
                var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
                if (mcsLeadPlayerPos == null)
                {
                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:leadPosNull");
                }

                var sqrDistance = BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos);
                if (sqrDistance >= TOO_FAR_FROM_LEAD_DISTANCE_WHEN_ESCORT * TOO_FAR_FROM_LEAD_DISTANCE_WHEN_ESCORT)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:WaitForLead");
                }
                else
                {
                    if (McsBotPlayerData != null && McsBotPlayerData.EscortPos != null)
                    {
                        BotOwner.GoToSomePointData.SetPoint(McsBotPlayerData.EscortPos.Value);
                        return new Action(typeof(EscortToPointLogic), "Mcs:EscortToPoint");
                    }
                    else
                    {
                        return new Action(typeof(SimplePatrolLogic), "Mcs:CannotFindEscortPos");
                    }
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