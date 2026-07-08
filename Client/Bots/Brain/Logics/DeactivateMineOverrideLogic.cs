using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class DeactivateMineOverrideLogic : DeactivateMineBaseLogic
    {
        public DeactivateMineOverrideLogic(BotOwner botOwner) : base(botOwner)
        {
            
        }

        public override void UpdateNodeByBrain(GClass26 data)
        {
            if (!BotOwner_0.BewarePlantedMine.CanDeactivate())
            {
                return;
            }

            var deactivatingPlace = BotOwner_0.BewarePlantedMine.DeactivatingPlace;
            if (deactivatingPlace == null)
            {
                return;
            }

            deactivatingPlace.SetDeactivate(BotOwner_0.Id);
            var sqrDistance = deactivatingPlace.Pos.McsSqrDistance(BotOwner_0.Position);
            BotOwner_0.Sprint(false, false);
            if (sqrDistance <= 5f)
            {
                method_6();
                BotOwner_0.SetPose(0.1f);
                BotOwner_0.StopMove();
                BotOwner_0.Steering.LookToPoint(deactivatingPlace.Pos);
            }
            else
            {
                BotOwner_0.SetTargetMoveSpeed(1f);
                BotOwner_0.Sprint(true, false);
                BotOwner_0.SetPose(1f);
                BotOwner_0.Steering.LookToMovingDirection();
                BetterSetDeactivatingPlacePos(deactivatingPlace.Pos);
            }
            method_0(false);
        }

        public virtual void BetterSetDeactivatingPlacePos(Vector3 pos)
        {
            if (Float_0 < Time.time)
            {
                if (Tools.BetterDestination(1.5f, pos, out var betterDestination))
                {
                    BotOwner_0.Mover.GoToPoint(betterDestination, false, 0.5f);
                }
                else
                {
                    BotOwner_0.Mover.GoToPoint(pos, false, 0.5f);
                }
                Float_0 = Time.time + 5f;
            }
        }
    }
}