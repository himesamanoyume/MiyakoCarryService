
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
            if (_owner.Medecine.Using)
            {
                return;
            }

            if (_owner.WeaponManager.Reload.Reloading)
            {
                _owner.WeaponManager.Reload.TryStopReload();
            }

            _owner.LookData.SetLookPointByHearing();
            var shallStartUse = _owner.Medecine.FirstAid.ShallStartUse();
            if (shallStartUse && _owner.Medecine.FirstAid.IsBleeding)
            {
                _baseLogic.UpdateNodeByMain(data);
                _owner.SetPose(1f);
                _owner.Medecine.FirstAid.TryApplyToCurrentPart();
            }
            else if (_owner.Medecine.SurgicalKit.ShallStartUse())
            {
                _owner.StopMove();
                _owner.SetPose(0f);
                _owner.Medecine.SurgicalKit.ApplyToCurrentPart();
            }
            else if (shallStartUse)
            {
                _baseLogic.UpdateNodeByMain(data);
                _owner.SetPose(1f);
                _owner.Medecine.FirstAid.TryApplyToCurrentPart();
            }
            _owner.Sprint(false);
        }
    }
}