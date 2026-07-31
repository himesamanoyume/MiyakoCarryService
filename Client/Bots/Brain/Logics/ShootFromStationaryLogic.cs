using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class ShootFromStationaryLogic : McsBotBaseLogic
    {
        private ShootFromStationaryBaseLogic _baseLogic;

        public ShootFromStationaryLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Stop()
        {
            base.Stop();
            if (BotOwner?.WeaponManager?.Stationary?.CurLink != null && BotOwner?.WeaponManager?.Stationary?.CheckAmmonProcess() == false)
            {
                BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.OnOutOfAmmo
                });
            }
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByMain(data);
        }
    }
}