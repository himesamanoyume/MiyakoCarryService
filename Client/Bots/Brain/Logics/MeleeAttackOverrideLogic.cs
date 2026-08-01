
using EFT;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class MeleeAttackOverrideLogic : OneMeleeAttackNode
    {
        private float _lastPathUpdateTime = 0f;
        private const float PATH_UPDATE_INTERVAL = 0.3f;
        private const float CUSTOM_MELEE_STOP_DISTANCE = 0.5f;
        private const float ATTACK_DISTANCE = 2.5f;
        private const float MOVE_SPEED_WHILE_ATTACKING = 0.3f;
        public MeleeAttackOverrideLogic(BotOwner bot) : base(bot)
        {

        }

        public override void UpdateNodeByBrain(CoreActionResultParams data)
        {
            var weaponManager = _owner.WeaponManager;
            var meleeData = weaponManager?.Melee;

            if (meleeData == null)
            {
                return;
            }

            if (!weaponManager.IsMelee)
            {
                if (!weaponManager.Selector.CanChangeToMeleeWeapons)
                {
                    return;
                }
                weaponManager.Selector.ChangeToMelee();
            }

            if (_owner.BotLay.IsLay)
            {
                _owner.BotLay.GetUp(false);
            }

            _owner.SetPose(1f);

            var goalEnemy = _owner.Memory.GoalEnemy;
            if (goalEnemy == null)
            {
                return;
            }

            var distance = goalEnemy.Distance;
            var inAttackRange = distance < ATTACK_DISTANCE;

            if (inAttackRange)
            {
                _owner.Steering.LookToPoint(goalEnemy._allParts[BodyPartType.head].Position);
                if (goalEnemy.Person.AIData.Player.MovementContext.IsInPronePose)
                {
                    _owner.SetPose(0f);
                }
            }
            else
            {
                _owner.Steering.LookToMovingDirection();
            }

            var shouldSprint = distance > meleeData.DIST_TO_STOP_SPRINT;
            _owner.Sprint(shouldSprint, false);

            if (meleeData._nextTryHitTime < Time.time)
            {
                TryMeleeAttack(goalEnemy);
                meleeData.ResetHitTime(meleeData.TRY_HIT_PERIOD_FALSE);
            }

            UpdateCustomMovement(goalEnemy, meleeData, distance, inAttackRange);
        }

        private bool TryMeleeAttack(EnemyInfo enemyInfo)
        {
            var weaponManager = _owner.WeaponManager;
            var meleeData = weaponManager?.Melee;

            if (meleeData == null)
            {
                return false;
            }

            if (meleeData.MeleeWeaponEquipped && Time.time - enemyInfo.PersonalLastSeenTime < 0.2f && meleeData.KnifeController != null)
            {
                var result = (!_owner.Settings.FileSettings.Shoot.ALTERNATIVE_KNIFE_KICK) ? meleeData.KnifeController.MakeKnifeKick() : meleeData.KnifeController.MakeAlternativeKick();
                return result;
            }

            return false;
        }

        private void UpdateCustomMovement(EnemyInfo goalEnemy, BotMeleeWeaponData meleeData, float distance, bool inAttackRange)
        {
            if (distance < CUSTOM_MELEE_STOP_DISTANCE)
            {
                if (inAttackRange)
                {
                    _owner.SetTargetMoveSpeed(MOVE_SPEED_WHILE_ATTACKING);

                    if (_lastPathUpdateTime < Time.time)
                    {
                        _lastPathUpdateTime = Time.time + PATH_UPDATE_INTERVAL;
                        var predictedPosition = PredictEnemyPosition(goalEnemy);
                        _owner.GoToPoint(predictedPosition, true, -1f, false, false, true, false, false);
                    }
                }
            }
            else
            {
                if (_lastPathUpdateTime < Time.time)
                {
                    _lastPathUpdateTime = Time.time + PATH_UPDATE_INTERVAL;

                    if (IsAlreadyMovingToTarget(goalEnemy.Person.Position))
                    {
                        return;
                    }

                    if (CanReachEnemy(meleeData, goalEnemy, out Vector3[] path))
                    {
                        if (path != null && path.Length > 0)
                        {
                            _owner.GoToByWay(path, -1f);
                        }
                    }
                }
            }
        }

        private bool IsAlreadyMovingToTarget(Vector3 targetPosition)
        {
            var pathController = _owner.Mover.ActualPathController;
            if (pathController == null || pathController.CurPath == null)
            {
                return false;
            }

            var currentTarget = pathController.CurPath.TargetPoint.Position;
            var distanceToTarget = (currentTarget - targetPosition).magnitude;

            if (distanceToTarget < 1f)
            {
                return true;
            }

            return false;
        }

        private Vector3 PredictEnemyPosition(EnemyInfo enemyInfo)
        {
            var enemyPos = enemyInfo.Person.Position;
            var enemyVelocity = enemyInfo.Person.Velocity;

            var predictedPos = enemyPos + enemyVelocity;

            if (Tools.BetterDestination(0.5f, predictedPos, out var betterDestination))
            {
                return betterDestination;
            }

            return predictedPos;
        }

        private bool CanReachEnemy(BotMeleeWeaponData meleeData, EnemyInfo enemy, out Vector3[] path)
        {
            path = null;

            if (meleeData != null && meleeData.CanRunToEnemyToHit(enemy, out Vector3[] way))
            {
                path = way;
                return true;
            }

            return false;
        }
    }
}