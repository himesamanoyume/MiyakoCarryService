using EFT;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class HealOverrideLogic : HealNode
    {
        private GoToSomePoint _baseLogic;

        public HealOverrideLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void UpdateNodeByBrain(CoreActionResultParams data)
        {
            if (!_owner.McsCanStartMed())
            {
                return;
            }

            if (_owner.WeaponManager.Reload.Reloading)
            {
                _owner.WeaponManager.Reload.TryStopReload();
            }

            _owner.LookData.SetLookPointByHearing();
            var firstAid = _owner.Medecine.FirstAid;
            var shallStartUse = firstAid.ShallStartUse();
            if (shallStartUse && firstAid.IsBleeding)
            {
                _baseLogic.UpdateNodeByMain(data);
                _owner.SetPose(1f);
                firstAid.TryApplyToCurrentPart();
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
                firstAid.TryApplyToCurrentPart();
            }
            _owner.Sprint(false);
        }
    }
}