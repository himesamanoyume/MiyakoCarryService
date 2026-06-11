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
                var sqrDistance = BotOwner.Position.McsSqrDistance(mcsBotPlayerData.LeadPlayer.Position);
                if (sqrDistance >= 11f * 11f && sqrDistance < 18f * 18f)
                {
                    BotOwner.Sprint(false, false);
                }
                else
                {
                    BotOwner.Sprint(true, false);
                }
            }
        }
    }
}