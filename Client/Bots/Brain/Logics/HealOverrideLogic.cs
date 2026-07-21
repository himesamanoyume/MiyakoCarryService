
using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class HealOverrideLogic : HealBaseLogic
    {
        private GoToPointBaseLogic _baseLogic;

        public HealOverrideLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void UpdateNodeByBrain(BaseIntent data)
        {
            if (BotOwner_0.Medecine.Using)
            {
                return;
            }

            if (BotOwner_0.WeaponManager.Reload.Reloading)
            {
                BotOwner_0.WeaponManager.Reload.TryStopReload();
            }

            BotOwner_0.LookData.SetLookPointByHearing();
            var shallStartUse = BotOwner_0.Medecine.FirstAid.ShallStartUse();
            if (shallStartUse && BotOwner_0.Medecine.FirstAid.IsBleeding)
            {
                _baseLogic.UpdateNodeByMain(data);
                BotOwner_0.SetPose(1f);
                BotOwner_0.Medecine.FirstAid.TryApplyToCurrentPart();
            }
            else if (BotOwner_0.Medecine.SurgicalKit.ShallStartUse())
            {
                BotOwner_0.StopMove();
                BotOwner_0.SetPose(0f);
                BotOwner_0.Medecine.SurgicalKit.ApplyToCurrentPart();
            }
            else if (shallStartUse)
            {
                _baseLogic.UpdateNodeByMain(data);
                BotOwner_0.SetPose(1f);
                BotOwner_0.Medecine.FirstAid.TryApplyToCurrentPart();
            }
            BotOwner_0.Sprint(false);
        }
    }
}