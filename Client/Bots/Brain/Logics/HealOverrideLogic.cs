
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
            if (botOwner_0.Medecine.Using)
            {
                return;
            }

            if (botOwner_0.WeaponManager.Reload.Reloading)
            {
                botOwner_0.WeaponManager.Reload.TryStopReload();
            }

            botOwner_0.LookData.SetLookPointByHearing();
            var shallStartUse = botOwner_0.Medecine.FirstAid.ShallStartUse();
            if (shallStartUse && botOwner_0.Medecine.FirstAid.IsBleeding)
            {
                _baseLogic.UpdateNodeByMain(data);
                botOwner_0.SetPose(1f);
                botOwner_0.Medecine.FirstAid.TryApplyToCurrentPart();
            }
            else if (botOwner_0.Medecine.SurgicalKit.ShallStartUse())
            {
                botOwner_0.StopMove();
                botOwner_0.SetPose(0f);
                botOwner_0.Medecine.SurgicalKit.ApplyToCurrentPart();
            }
            else if (shallStartUse)
            {
                _baseLogic.UpdateNodeByMain(data);
                botOwner_0.SetPose(1f);
                botOwner_0.Medecine.FirstAid.TryApplyToCurrentPart();
            }
            botOwner_0.Sprint(false);
        }
    }
}