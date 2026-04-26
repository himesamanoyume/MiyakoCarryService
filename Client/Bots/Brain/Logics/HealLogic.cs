
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class HealLogic : McsBotBaseLogic
    {
        private HealBaseLogic _baseLogic;

        public HealLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Start()
        {
            base.Start();
            BotOwner.TalkMsg(EPhraseTrigger.StartHeal);
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            // if (Random.Range(0, 100) > 90)
            // {
            //     BotOwner.ShowSubtitleMsg(string.Format("<b>{0}</b>:正在治疗!".McsLocalized(), BotOwner.Profile.Nickname));
            // }
            _baseLogic.UpdateNodeByMain(data);
        }
    }
}