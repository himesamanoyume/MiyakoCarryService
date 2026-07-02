
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class AttackMovingLogic : McsBotBaseLogic
    {
        private AttackMovingOverrideLogic _baseLogic;

        public AttackMovingLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByMain(data);
        }

        public sealed class AttackMovingOverrideLogic : AttackMovingBaseLogic
        {
            private float _lastPathUpdateTime = 0f;
            private const float PATH_UPDATE_INTERVAL = 0.5f;

            public AttackMovingOverrideLogic(BotOwner bot) : base(bot)
            {
                
            }

            public override void UpdateNodeByBrain(BaseIntent data)
            {
                method_0();
                BotOwner_0.SetTargetMoveSpeed(1f);
                BotOwner_0.Sprint(false, false);
                BotOwner_0.SetPose(1f);

                var goalEnemy = BotOwner_0.Memory.GoalEnemy;
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

                    var directionToEnemy = (goalEnemy.Person.Position - BotOwner_0.Position).normalized;
                    var targetPosition = goalEnemy.Person.Position - directionToEnemy * 3f;

                    BotOwner_0.GoToPoint(targetPosition, true, -1f, false, false, true, false, false);
                }

                BotOwner_0.Steering.LookToMovingDirection();
            }

            public override void AimingAndShoot(BaseIntent data)
            {
                var goalEnemy = BotOwner_0.Memory.GoalEnemy;
                if (goalEnemy != null && goalEnemy.CanShoot && goalEnemy.IsVisible)
                {
                    if (BotOwner_0.WeaponManager.UnderbarrelLauncherController.CanSwitchInFight(BotOwner_0))
                    {
                        BotOwner_0.WeaponManager.UnderbarrelLauncherController.TryEnable(null);
                    }
                    // 使用原版的瞄准逻辑  
                    base.AimingAndShoot(data);
                    return;
                }
                BotOwner_0.LookData.SetLookPointByHearing(null);
            }
        }
    }
}