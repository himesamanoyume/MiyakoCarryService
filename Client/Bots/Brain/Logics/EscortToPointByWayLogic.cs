using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class EscortToPointByWayLogic : McsBotBaseLogic
    {
        private GoToPointBaseLogic _baseLogic;

        public EscortToPointByWayLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                BotOwner.Sprint(true, false);
                _baseLogic.UpdateNodeByMain(data);
                return;
            }

            var leadPlayer = mcsBotPlayerData.LeadPlayer;
            var botToLeaderSqrDistance = BotOwner.Position.McsSqrDistance(leadPlayer.Position);
            var botToTargetSqrDistance = BotOwner.Position.McsSqrDistance(mcsBotPlayerData.TargetPos.Value);
            var leaderToTargetSqrDistance = leadPlayer.Position.McsSqrDistance(mcsBotPlayerData.TargetPos.Value);
            BotOwner.Steering.LookToMovingDirection();

            var leaderMovementContext = leadPlayer.MovementContext;
            var botWithin50 = botToLeaderSqrDistance < 50f * 50f;
            var leaderRelativeSpeed = leaderMovementContext.MaxSpeed > 0f ? leaderMovementContext.CharacterMovementSpeed / leaderMovementContext.MaxSpeed : 0f;

            if (!BotOwner.Memory.HaveEnemy && botWithin50 && leaderMovementContext.IsInPronePose)
            {
                _baseLogic.method_0();
                BotOwner.GoToSomePointData.UpdateToGo(false, 0f, leaderMovementContext.PoseLevel);
            }
            else if (!BotOwner.Memory.HaveEnemy && botWithin50 && (leaderMovementContext.PoseLevel < 1f || leaderMovementContext.PoseLevel == 1f && leaderRelativeSpeed < 1f))
            {
                _baseLogic.method_0();
                BotOwner.GoToSomePointData.UpdateToGo(false, leaderMovementContext.CharacterMovementSpeed, leaderMovementContext.PoseLevel);
            }
            else
            {
                if (leaderToTargetSqrDistance < botToTargetSqrDistance || botToLeaderSqrDistance <= 11f * 11f || botToLeaderSqrDistance >= 18f * 18f)
                {
                    BotOwner.Sprint(true, false);
                }
                else
                {
                    BotOwner.Sprint(false, false);
                }
                _baseLogic.UpdateNodeByMain(data);
            }
        }
    }
}