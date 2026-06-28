using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class EscortToPointByWayLogic : McsBotBaseLogic
    {
        private GoToPointBaseLogic _baseLogic;

        public EscortToPointByWayLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByMain(data);
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                var botToLeaderSqrDistance = BotOwner.Position.McsSqrDistance(mcsBotPlayerData.LeadPlayer.Position);
                var botToTargetSqrDistance = BotOwner.Position.McsSqrDistance(mcsBotPlayerData.TargetPos.Value);
                var leaderToTargetSqrDistance = mcsBotPlayerData.LeadPlayer.Position.McsSqrDistance(mcsBotPlayerData.TargetPos.Value);
                BotOwner.Steering.LookToMovingDirection();
                
                if (leaderToTargetSqrDistance < botToTargetSqrDistance || botToLeaderSqrDistance <= 11f * 11f || botToLeaderSqrDistance >= 18f * 18f)
                {
                    BotOwner.Sprint(true, false);
                }
                else
                {
                    BotOwner.Sprint(false, false);
                }
            }
        }
    }
}