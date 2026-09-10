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
    public class StandAndShootLogic : McsBotBaseLogic
    {
        private ShootFromPlace _baseLogic;
        private float _nextStrafeCheckTime = 0f;
        private float _strafeUntilTime = 0f;
        private Vector3 _strafeTarget = Vector3.zero;
        private const float STRAFE_DURATION = 2.5f;

        public StandAndShootLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy == null)
            {
                _baseLogic.UpdateNodeByMain(data);
                return;
            }

            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null && mcsBotPlayerData.HasAnyIntent(Intents.ShouldHoldPosition, Intents.ShouldFollowMe, Intents.ShouldKeepFormation))
            {
                if (_strafeTarget != Vector3.zero)
                {
                    _strafeTarget = Vector3.zero;
                    _strafeUntilTime = 0f;
                    BotOwner.StopMove();
                }
                _baseLogic.UpdateNodeByMain(data);
                return;
            }

            if (_strafeTarget != Vector3.zero && Time.time < _strafeUntilTime)
            {
                UpdateStrafe(goalEnemy);
                return;
            }

            _strafeTarget = Vector3.zero;

            if (_nextStrafeCheckTime < Time.time)
            {
                _nextStrafeCheckTime = Time.time + UnityEngine.Random.Range(2.5f, 5f);
                if (goalEnemy.IsVisible && TryFindStrafePoint(goalEnemy, out var strafeTarget))
                {
                    _strafeTarget = strafeTarget;
                    _strafeUntilTime = Time.time + STRAFE_DURATION;
                    return;
                }
            }

            _baseLogic.UpdateNodeByMain(data);
        }

        private void UpdateStrafe(EnemyInfo goalEnemy)
        {
            BotOwner.Sprint(false, false);
            BotOwner.SetTargetMoveSpeed(1f);

            if (_nextStrafeCheckTime - STRAFE_DURATION + 0.4f < Time.time)
            {
                BotOwner.GoToPoint(_strafeTarget, true, -1f, false, false, true, false, false);
            }

            if (BotOwner.Position.McsSqrDistance(_strafeTarget) <= (0.75f * 0.75f))
            {
                _strafeTarget = Vector3.zero;
                _strafeUntilTime = 0f;
                return;
            }

            BotOwner.Steering.LookToMovingDirection();
            if (goalEnemy.IsVisible)
            {
                AimAndShootAtPoint(goalEnemy.GetPartToShoot());
            }
        }

        private bool TryFindStrafePoint(EnemyInfo goalEnemy, out Vector3 strafeTarget)
        {
            strafeTarget = Vector3.zero;
            var directionToEnemy = (goalEnemy.Person.Position - BotOwner.Position).normalized;
            var rotationAngle = Random.Range(70f, 110f) * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
            var strafeDirection = Quaternion.Euler(0f, rotationAngle, 0f) * directionToEnemy;
            var rawTarget = BotOwner.Position + strafeDirection * 6f;
            rawTarget.y = BotOwner.Position.y;

            var origin = BotOwner.Position + Vector3.up * 0.5f;
            var rayDirection = (rawTarget - origin).normalized;
            var rayDistance = Vector3.Distance(origin, rawTarget);
            if (NavMesh.Raycast(origin, origin + rayDirection * rayDistance, out var navMeshRayHit, NavMesh.AllAreas))
            {
                rawTarget = navMeshRayHit.position;
            }

            if (BotOwner.Position.McsSqrDistance(rawTarget) <= 1f)
            {
                return false;
            }

            if (!NavMesh.SamplePosition(rawTarget, out var navMeshHit, 1.5f, NavMesh.AllAreas))
            {
                return false;
            }

            strafeTarget = navMeshHit.position;
            return true;
        }
    }
}