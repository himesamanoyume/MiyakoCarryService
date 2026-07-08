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

        public override void UpdateNodeByBrain(CoreActionResultParams data)
        {
            if (!botOwner_0.BewarePlantedMine.CanDeactivate())
            {
                return;
            }

            var deactivatingPlace = botOwner_0.BewarePlantedMine.DeactivatingPlace;
            if (deactivatingPlace == null)
            {
                return;
            }

            deactivatingPlace.SetDeactivate(botOwner_0.Id);
            var sqrDistance = deactivatingPlace.Pos.McsSqrDistance(botOwner_0.Position);
            botOwner_0.Sprint(false, false);
            if (sqrDistance <= 5f)
            {
                DoDeactivateProcess();
                botOwner_0.SetPose(0.1f);
                botOwner_0.StopMove();
                botOwner_0.Steering.LookToPoint(deactivatingPlace.Pos);
            }
            else
            {
                botOwner_0.SetTargetMoveSpeed(1f);
                botOwner_0.Sprint(true, false);
                botOwner_0.SetPose(1f);
                botOwner_0.Steering.LookToMovingDirection();
                BetterSetDeactivatingPlacePos(deactivatingPlace.Pos);
            }
            DoorOpen(false);
        }

        public virtual void BetterSetDeactivatingPlacePos(Vector3 pos)
        {
            if (float_0 < Time.time)
            {
                if (Tools.BetterDestination(1.5f, pos, out var betterDestination))
                {
                    botOwner_0.Mover.GoToPoint(betterDestination, false, 0.5f);
                }
                else
                {
                    botOwner_0.Mover.GoToPoint(pos, false, 0.5f);
                }
                float_0 = Time.time + 5f;
            }
        }
    }
}