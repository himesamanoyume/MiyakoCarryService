using System;
using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class HealOverrideLogic : HealNode
    {
        private static readonly Action McsHealCallback = () => { };

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

            var reload = Tools.GetReload(_owner);
            if (reload != null && reload.Reloading)
            {
                reload.TryStopReload();
            }

            _owner.LookData.SetLookPointByHearing();
            var firstAid = _owner.Medecine.FirstAid;
            var shallStartUse = firstAid.ShallStartUse();

            if (Tools.GetWeaponInfo(_owner) == null)
            {
                return;
            }

            if (shallStartUse && firstAid.IsBleeding)
            {
                _baseLogic.UpdateNodeByMain(null);
                _owner.SetPose(1f);
                firstAid.TryApplyToCurrentPart(new Il2CppSystem.Nullable<int>(), McsHealCallback);
            }
            else if (_owner.Medecine.SurgicalKit.ShallStartUse())
            {
                _owner.StopMove();
                _owner.SetPose(0f);
                _owner.Medecine.SurgicalKit.ApplyToCurrentPart(McsHealCallback);
            }
            else if (shallStartUse)
            {
                _baseLogic.UpdateNodeByMain(null);
                _owner.SetPose(1f);
                firstAid.TryApplyToCurrentPart(new Il2CppSystem.Nullable<int>(), McsHealCallback);
            }
            _owner.Sprint(false);
        }
    }
}