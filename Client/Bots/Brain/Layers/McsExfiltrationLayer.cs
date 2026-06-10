
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsExfiltrationLayer : McsBaseLayer<McsExfiltrationLayer>
    {
        // 替换GClass75
        public McsExfiltrationLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            
        }

        public override void Start()
        {
            base.Start();
            if (McsBotPlayerData != null)
            {
                McsBotPlayerData.IsLooting = false;
                McsBotPlayerData.EscortPos = null;
            }
        }

        private float _lastPositionUpdateTime = 0f;
        private Vector3 _lastRecordedPosition = new(0,0,0);

        public override Action GetNextAction()
        {
            if (BotOwner.PatrollingData.ExfiltrationData.HaveActions())
            {
                return new Action(typeof(GoToExfiltrationPointNodeLogic), "Mcs:GotoExit");
            }
            return new Action(typeof(HoldPositionLogic), "Mcs:HoldExf");
        }

        public override bool IsActive()
        {
            if (IsMcsBotPlayer)
            {
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
                    return true;
                }

                if (McsBotPlayerData.HasDecision(EDecision.ShouldExfil))
                {
                    return true;
                }

                return false;
            }

            if (IsWannaLeave() && !BotOwner.Memory.HaveEnemy && BotOwner.PatrollingData.ExfiltrationData.HaveActions())
            {
                BotOwner.Exfiltration.ResetLeaveTime();
                return true;
            }

            var timeSinceLastPositionUpdate = Time.time - _lastPositionUpdateTime;

            if (timeSinceLastPositionUpdate >= 25f && timeSinceLastPositionUpdate <= 60f && !BotOwner.Exfiltration.BotInExfiltrationArea())
            {
                BotOwner.Exfiltration.ResetLeaveTime();
                return false;
            }

            if (BotOwner.Position.McsSqrDistance(_lastRecordedPosition) > 1f)
            {
                _lastRecordedPosition = BotOwner.Position;
                _lastPositionUpdateTime = Time.time;
            }

            return false;
        }
    }
}