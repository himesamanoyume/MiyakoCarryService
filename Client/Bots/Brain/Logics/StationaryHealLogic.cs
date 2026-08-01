
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class StationaryHealLogic : McsBotBaseLogic
    {
        private HealNode _baseLogic;

        public StationaryHealLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Start()
        {
            base.Start();
            BotOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.StartHeal,
            });
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByBrain(data);
        }
    }
}