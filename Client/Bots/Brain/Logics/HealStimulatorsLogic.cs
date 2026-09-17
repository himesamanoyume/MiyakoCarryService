using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class HealStimulatorsLogic : McsBotBaseLogic
    {
        private StimulatorsNode _baseLogic;

        public HealStimulatorsLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            if (!BotOwner.McsCanStartMed())
            {
                return;
            }

            _baseLogic.UpdateNodeByMain(data);
        }
    }
}