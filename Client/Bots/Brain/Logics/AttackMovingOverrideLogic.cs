
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class AttackMovingOverrideLogic : AttackMoving
    {
        private float _lastPathUpdateTime = 0f;
        private const float PATH_UPDATE_INTERVAL = 0.5f;

        public AttackMovingOverrideLogic(BotOwner bot) : base(bot)
        {

        }

        public override void UpdateNodeByBrain(CoreActionResultParams data)
        {
            var goalEnemy = _owner.Memory.GoalEnemy;
            if (goalEnemy == null)
            {
                return;
            }

            if (goalEnemy.IsVisible)
            {
                base.UpdateNodeByBrain(data);
                return;
            }

            DoorOpen();
            _owner.SetTargetMoveSpeed(1f);
            _owner.Sprint(false, false);
            _owner.SetPose(1f);
            MoveTowardsEnemy(goalEnemy);
            AimingAndShoot(data);
        }

        private void MoveTowardsEnemy(EnemyInfo goalEnemy)
        {
            if (_lastPathUpdateTime < Time.time)
            {
                _lastPathUpdateTime = Time.time + PATH_UPDATE_INTERVAL;

                var directionToEnemy = (goalEnemy.Person.Position - _owner.Position).normalized;
                var targetPosition = goalEnemy.Person.Position - directionToEnemy * 3f;

                _owner.GoToPoint(targetPosition, true, -1f, false, false, true, false, false);
            }

            _owner.Steering.LookToMovingDirection();
        }

        public override void AimingAndShoot(CoreActionResultParams data)
        {
            var goalEnemy = _owner.Memory.GoalEnemy;
            if (goalEnemy != null && goalEnemy.CanShoot && goalEnemy.IsVisible)
            {
                if (_owner.WeaponManager.UnderbarrelLauncherController.CanSwitchInFight(_owner))
                {
                    _owner.WeaponManager.UnderbarrelLauncherController.TryEnable(null);
                }
                // 使用原版的瞄准逻辑  
                base.AimingAndShoot(data);
                return;
            }
            _owner.LookData.SetLookPointByHearing(null);
        }
    }
}