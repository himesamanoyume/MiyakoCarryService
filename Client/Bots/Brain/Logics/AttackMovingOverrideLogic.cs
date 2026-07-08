
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class AttackMovingOverrideLogic : AttackMovingBaseLogic
    {
        private float _lastPathUpdateTime = 0f;
        private const float PATH_UPDATE_INTERVAL = 0.5f;

        public AttackMovingOverrideLogic(BotOwner bot) : base(bot)
        {

        }

        public override void UpdateNodeByBrain(BaseIntent data)
        {
            DoorOpen();
            botOwner_0.SetTargetMoveSpeed(1f);
            botOwner_0.Sprint(false, false);
            botOwner_0.SetPose(1f);

            var goalEnemy = botOwner_0.Memory.GoalEnemy;
            if (goalEnemy == null)
            {
                return;
            }

            MoveTowardsEnemy(goalEnemy);
            AimingAndShoot(data);
        }

        private void MoveTowardsEnemy(EnemyInfo goalEnemy)
        {
            if (_lastPathUpdateTime < Time.time)
            {
                _lastPathUpdateTime = Time.time + PATH_UPDATE_INTERVAL;

                var directionToEnemy = (goalEnemy.Person.Position - botOwner_0.Position).normalized;
                var targetPosition = goalEnemy.Person.Position - directionToEnemy * 3f;

                botOwner_0.GoToPoint(targetPosition, true, -1f, false, false, true, false, false);
            }

            botOwner_0.Steering.LookToMovingDirection();
        }

        public override void AimingAndShoot(BaseIntent data)
        {
            var goalEnemy = botOwner_0.Memory.GoalEnemy;
            if (goalEnemy != null && goalEnemy.CanShoot && goalEnemy.IsVisible)
            {
                if (botOwner_0.WeaponManager.UnderbarrelLauncherController.CanSwitchInFight(botOwner_0))
                {
                    botOwner_0.WeaponManager.UnderbarrelLauncherController.TryEnable(null);
                }
                // 使用原版的瞄准逻辑  
                base.AimingAndShoot(data);
                return;
            }
            botOwner_0.LookData.SetLookPointByHearing(null);
        }
    }
}