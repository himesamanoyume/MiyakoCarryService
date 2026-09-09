using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    /// <summary>
    /// 重点参考了SAIN
    /// </summary>
    public class McsDogFightLogic : McsBotBaseLogic
    {
        private bool _dogFightPhaseIsShooting = false;
        private float _dogFightPhaseEndTime = 0f;
        private int _dogFightTiltDirection = 1;
        private Vector3 _backupTarget = Vector3.zero;
        private float _nextJumpTime = 0f;
        private float _nextPathUpdateTime = 0f;
        private const float STRAFE_SPEED = 0.5f;
        private const float BACKUP_DISTANCE = 3f;
        private const float BACKUP_RANDOM_RADIUS = 2f;
        private const float PATH_UPDATE_INTERVAL = 0.4f;

        public McsDogFightLogic(BotOwner botOwner) : base(botOwner)
        {
            
        }

        public override void Start()
        {
            base.Start();
            _dogFightPhaseIsShooting = false;
            _dogFightPhaseEndTime = 0f;
            _dogFightTiltDirection = Random.value < 0.5f ? -1 : 1;
            _backupTarget = Vector3.zero;
            _nextJumpTime = 0f;
            _nextPathUpdateTime = 0f;
        }

        public override void Stop()
        {
            TiltToSide(0);
            base.Stop();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy == null)
            {
                return;
            }

            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null && mcsBotPlayerData.HasAnyIntent(Intents.ShouldHoldPosition, Intents.ShouldFollowMe, Intents.ShouldKeepFormation))
            {
                TiltToSide(0);
                BotOwner.StopMove();
                return;
            }

            if (_dogFightPhaseIsShooting)
            {
                UpdateShootingPhase(goalEnemy);
            }
            else
            {
                if (_backupTarget != Vector3.zero)
                {
                    UpdateBackingUpPhase(goalEnemy);
                }
                else
                {
                    UpdateMovingToEnemyPhase(goalEnemy);
                }
            }
        }

        private void UpdateMovingToEnemyPhase(EnemyInfo goalEnemy)
        {
            BotOwner.Sprint(false, false);
            BotOwner.SetTargetMoveSpeed(STRAFE_SPEED);
            BotOwner.SetPose(1f);

            var botToEnemySqrDist = BotOwner.Position.McsSqrDistance(goalEnemy.Person.Position);

            if (goalEnemy.IsVisible && botToEnemySqrDist <= (8f * 8f))
            {
                SwitchToShootingPhase(Random.Range(0.4f, CalcShootDurationMax()));
                BotOwner.StopMove();
                return;
            }

            if (_nextPathUpdateTime < Time.time)
            {
                _nextPathUpdateTime = Time.time + PATH_UPDATE_INTERVAL;
                BotOwner.GoToPoint(goalEnemy.Person.Position, true, -1f, false, false, true, false, false);
            }

            if (goalEnemy.IsVisible
                && botToEnemySqrDist >= (1.5f * 1.5f)
                && botToEnemySqrDist <= (6f * 6f)
                && Time.time >= _nextJumpTime
                && MyExtensions.IsTrue100(35f))
            {
                _nextJumpTime = Time.time + 3f;
                BotOwner.GetPlayer?.MovementContext?.TryJump();
            }

            if (goalEnemy.IsVisible)
            {
                AimAndShootAtPoint(goalEnemy.GetPartToShoot());
            }
            else
            {
                BotOwner.Steering.LookToMovingDirection();
            }
        }

        private void UpdateShootingPhase(EnemyInfo goalEnemy)
        {
            if (!goalEnemy.IsVisible || Time.time > _dogFightPhaseEndTime)
            {
                SwitchToMovingPhase(goalEnemy, Random.Range(0.75f, 1.1f));
                return;
            }

            TiltToSide(_dogFightTiltDirection);
            AimAndShootAtPoint(goalEnemy.GetPartToShoot());
        }

        private void UpdateBackingUpPhase(EnemyInfo goalEnemy)
        {
            BotOwner.Sprint(false, false);
            BotOwner.SetTargetMoveSpeed(1f);

            if (Time.time > _dogFightPhaseEndTime)
            {
                SwitchToMovingPhase(goalEnemy, 0f);
                return;
            }

            if (_nextPathUpdateTime < Time.time)
            {
                _nextPathUpdateTime = Time.time + PATH_UPDATE_INTERVAL;
                BotOwner.GoToPoint(_backupTarget, true, -1f, false, false, true, false, false);
            }

            if (goalEnemy.IsVisible)
            {
                TiltToSide(_dogFightTiltDirection);
                AimAndShootAtPoint(goalEnemy.GetPartToShoot());
            }
            else
            {
                BotOwner.Steering.LookToMovingDirection();
            }
        }

        private float CalcShootDurationMax()
        {
            var aimingSettings = BotOwner?.Settings?.FileSettings?.Aiming;
            if (aimingSettings != null)
            {
                return aimingSettings.MAX_AIM_TIME * 2f + 0.2f;
            }

            return 0.8f;
        }

        private void SwitchToShootingPhase(float duration)
        {
            _dogFightPhaseIsShooting = true;
            _dogFightPhaseEndTime = Time.time + duration;
            _backupTarget = Vector3.zero;
            if (Random.value < 0.5f)
            {
                _dogFightTiltDirection = -_dogFightTiltDirection;
            }
        }

        private void SwitchToMovingPhase(EnemyInfo goalEnemy, float duration)
        {
            _dogFightPhaseIsShooting = false;
            _dogFightPhaseEndTime = Time.time + duration;
            TiltToSide(0);
            if (duration > 0f)
            {
                CalcBackupTarget(goalEnemy);
                if (Random.value < 0.5f)
                {
                    _dogFightTiltDirection = -_dogFightTiltDirection;
                }
            }
            else
            {
                _backupTarget = Vector3.zero;
            }
        }

        private void CalcBackupTarget(EnemyInfo goalEnemy)
        {
            var directionToEnemy = (goalEnemy.Person.Position - BotOwner.Position).normalized;
            var backupDirection = -directionToEnemy;
            var rawTarget = BotOwner.Position + backupDirection * BACKUP_DISTANCE + Random.insideUnitSphere * BACKUP_RANDOM_RADIUS;
            rawTarget.y = BotOwner.Position.y;

            if (NavMesh.SamplePosition(rawTarget, out var navMeshHit, BACKUP_RANDOM_RADIUS, NavMesh.AllAreas))
            {
                _backupTarget = navMeshHit.position;
                return;
            }

            _backupTarget = BotOwner.Position + backupDirection * BACKUP_DISTANCE;
        }
    }
}