using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class RunAwayArtilleryLogic : McsBotBaseLogic
    {
        private RunAwayArtillery _baseLogic;

        public RunAwayArtilleryLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByMain(data);
        }
    }
}