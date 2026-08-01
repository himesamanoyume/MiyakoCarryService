using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class DeactivateMineOverrideLogic : DeactivateMineNode
    {
        public DeactivateMineOverrideLogic(BotOwner botOwner) : base(botOwner)
        {
            
        }

        public override void UpdateNodeByBrain(CoreActionResultParams data)
        {
            if (!_owner.BewarePlantedMine.CanDeactivate())
            {
                return;
            }

            var deactivatingPlace = _owner.BewarePlantedMine.DeactivatingPlace;
            if (deactivatingPlace == null)
            {
                return;
            }

            deactivatingPlace.SetDeactivate(_owner.Id);
            var sqrDistance = deactivatingPlace.Pos.McsSqrDistance(_owner.Position);
            _owner.Sprint(false, false);
            if (sqrDistance <= 5f)
            {
                DoDeactivateProcess();
                _owner.SetPose(0.1f);
                _owner.StopMove();
                _owner.Steering.LookToPoint(deactivatingPlace.Pos);
            }
            else
            {
                _owner.SetTargetMoveSpeed(1f);
                _owner.Sprint(true, false);
                _owner.SetPose(1f);
                _owner.Steering.LookToMovingDirection();
                BetterSetDeactivatingPlacePos(deactivatingPlace.Pos);
            }
            DoorOpen(false);
        }

        public virtual void BetterSetDeactivatingPlacePos(Vector3 pos)
        {
            if (_nextAct < Time.time)
            {
                if (Tools.BetterDestination(1.5f, pos, out var betterDestination))
                {
                    _owner.Mover.GoToPoint(betterDestination, false, 0.5f);
                }
                else
                {
                    _owner.Mover.GoToPoint(pos, false, 0.5f);
                }
                _nextAct = Time.time + 5f;
            }
        }
    }
}