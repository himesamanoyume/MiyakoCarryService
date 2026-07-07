
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsExfiltrationLayer : McsBaseLayer
    {
        public McsExfiltrationLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {

        }

        public override void Start()
        {
            base.Start();
            if (McsBotPlayerData != null)
            {
                McsBotPlayerData.IsLooting = false;
                McsBotPlayerData.TargetPos = null;
                McsBotPlayerData.ProxyTargetId = null;
            }
        }

        public override Action GetNextAction()
        {
            try
            {
                if (BotOwner.PatrollingData.ExfiltrationData.HaveActions())
                {
                    return new Action(typeof(GoToExfiltrationPointNodeLogic), "Mcs:GotoExit");
                }
                return new Action(typeof(HoldPositionLogic), "Mcs:HoldExf");
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return new Action(typeof(SimplePatrolLogic), "Mcs:Exception");
            }
        }

        public override bool IsActive()
        {
            if (IsMcsBotPlayer)
            {
#if DEBUG
                if (!MiyakoCarryServicePlugin.EnableMcsLayer.Value)
                {
                    return false;
                }
#endif

                if (McsBotPlayerData == null)
                {
                    return false;
                }

                if (BotOwner.Memory.HaveEnemy)
                {
                    return false;
                }

                if (BotOwner.Memory.IsUnderFire)
                {
                    return false;
                }

                if ((BotOwner.Medecine.FirstAid.Damaged && BotOwner.Medecine.FirstAid.HaveSmth2Use) || (BotOwner.Medecine.SurgicalKit.Damaged && BotOwner.Medecine.SurgicalKit.HaveSmth2Use))
                {
                    return false;
                }

                if (!McsBotPlayerData.LeadPlayer.HealthController.IsAlive)
                {
                    return true;
                }

                if (McsBotPlayerData.HasDecision(Decisions.ShouldExfil))
                {
                    return true;
                }

                return false;
            }

            return false;
        }
    }
}