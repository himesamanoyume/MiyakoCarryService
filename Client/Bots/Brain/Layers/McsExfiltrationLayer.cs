
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal class McsExfiltrationLayer : McsBaseLayer<McsExfiltrationLayer>
    {
        // 替换GClass75
        public McsExfiltrationLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            InitActionMap();
        }

        protected override void InitActionMap()
        {
            _endActionMap = new()
            {
                { typeof(HoldPositionLogic), EndHoldPosition },
                { typeof(GoToExfiltrationPointNodeLogic), EndGoToExfiltrationPoint }
            };
        }

        private float _lastPositionUpdateTime = 0f;
        private Vector3 _lastRecordedPosition = new(0,0,0);

        public override Action GetNextAction()
        {
            if (BotOwner.PatrollingData.ExfiltrationData.HaveActions())
            {
                return new Action(typeof(GoToExfiltrationPointNodeLogic), "Mcs:gotoExit");
            }
            return new Action(typeof(HoldPositionLogic), "Mcs:holdExf");
        }

        public override bool IsActive()
        {
            if (BotOwner.BotFollower.HaveBoss && IsMcsBotPlayer)
            {
                return false;
            }

            if (IsWannaLeave() && !BotOwner.Memory.HaveEnemy && BotOwner.PatrollingData.ExfiltrationData.HaveActions())
            {
                BotOwner.Exfiltration.ResetLeaveTime();
                return false;
            }

            var timeSinceLastPositionUpdate = Time.time - _lastPositionUpdateTime;

            if (timeSinceLastPositionUpdate >= 25f && timeSinceLastPositionUpdate <= 60f && !BotOwner.Exfiltration.BotInExfiltrationArea())
            {
                BotOwner.Exfiltration.ResetLeaveTime();
                return false;
            }

            if (Vector3.SqrMagnitude(BotOwner.Position - _lastRecordedPosition) > 1f)
            {
                _lastRecordedPosition = BotOwner.Position;
                _lastPositionUpdateTime = Time.time;
            }

            return true;
        }

        private bool IsWannaLeave()
        {
            if (BotOwner.Boss.IamBoss || BotOwner.BotFollower == null || BotOwner.BotFollower.BossToFollow == null)
            {
                return BotOwner.Exfiltration.WannaLeave();
            }
            IPlayer player = BotOwner.BotFollower.BossToFollow.Player();
            if (player != null && player.AIData != null && !(player.AIData.BotOwner == null) && player.AIData.BotOwner.Exfiltration != null)
            {
                return player.AIData.BotOwner.Exfiltration.WannaLeave();
            }
            return BotOwner.Exfiltration.WannaLeave();
        }

        private bool EndHoldPosition()
        {
            return true;
        }

        private bool EndGoToExfiltrationPoint()
        {
            return true;
        }
    }
}