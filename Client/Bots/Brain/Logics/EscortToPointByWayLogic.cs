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
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                return;
            }

            var sqrDistance = BotOwner.Position.McsSqrDistance(mcsBotPlayerData.LeadPlayer.Position);
            BotOwner.SetTargetMoveSpeed(1f);

            if (sqrDistance >= 8f * 8f && sqrDistance < 18f * 18f)
            {
                BotOwner.Sprint(false, true);
            }
            else
            {
                BotOwner.Sprint(true, true);
            }
            _baseLogic.UpdateNodeByMain(data);
        }
    }
}