
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class MeleeAttackLogic : McsBotBaseLogic
    {
        private MeleeAttackOverrideLogic _baseLogic;
        public MeleeAttackLogic(BotOwner botOwner) : base(botOwner)
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

        public sealed class MeleeAttackOverrideLogic : MeleeAttackBaseLogic
        {
            private float _lastPathUpdateTime = 0f;
            private const float PATH_UPDATE_INTERVAL = 0.3f;
            private const float CUSTOM_MELEE_STOP_DISTANCE = 0.5f;
            private const float ATTACK_DISTANCE = 2.5f;
            private const float MOVE_SPEED_WHILE_ATTACKING = 0.3f;
            public MeleeAttackOverrideLogic(BotOwner bot) : base(bot)
            {

            }

            public override void UpdateNodeByBrain(BaseIntent data)
            {
                var weaponManager = BotOwner_0.WeaponManager;
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

                if (BotOwner_0.BotLay.IsLay)
                {
                    BotOwner_0.BotLay.GetUp(false);
                }

                BotOwner_0.SetPose(1f);

                var goalEnemy = BotOwner_0.Memory.GoalEnemy;
                if (goalEnemy == null)
                {
                    return;
                }

                var distance = goalEnemy.Distance;
                var inAttackRange = distance < ATTACK_DISTANCE;

                if (inAttackRange)
                {
                    BotOwner_0.Steering.LookToPoint(goalEnemy.AllParts[BodyPartType.head].Position);
                    if (goalEnemy.Person.AIData.Player.MovementContext.IsInPronePose)
                    {
                        BotOwner_0.SetPose(0f);
                    }
                }
                else
                {
                    BotOwner_0.Steering.LookToMovingDirection();
                }

                var shouldSprint = distance > meleeData.Single_1;
                BotOwner_0.Sprint(shouldSprint, false);

                if (meleeData.NextTryHitTime < Time.time)
                {
                    var success = TryMeleeAttack(goalEnemy);
                    meleeData.method_0(meleeData.TRY_HIT_PERIOD_FALSE);
                }

                UpdateCustomMovement(goalEnemy, meleeData, distance, inAttackRange);
            }

            private bool TryMeleeAttack(EnemyInfo enemyInfo)
            {
                var weaponManager = BotOwner_0.WeaponManager;
                var meleeData = weaponManager?.Melee;

                if (meleeData == null)
                {
                    return false;
                }

                if (meleeData.MeleeWeaponEquipped && Time.time - enemyInfo.PersonalLastSeenTime < 0.2f && meleeData.KnifeController != null)
                {
                    var result = (!BotOwner_0.Settings.FileSettings.Shoot.ALTERNATIVE_KNIFE_KICK) ? meleeData.KnifeController.MakeKnifeKick() : meleeData.KnifeController.MakeAlternativeKick();
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
                        BotOwner_0.SetTargetMoveSpeed(MOVE_SPEED_WHILE_ATTACKING);

                        if (_lastPathUpdateTime < Time.time)
                        {
                            _lastPathUpdateTime = Time.time + PATH_UPDATE_INTERVAL;
                            var predictedPosition = PredictEnemyPosition(goalEnemy);
                            BotOwner_0.GoToPoint(predictedPosition, true, -1f, false, false, true, false, false);
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
                                BotOwner_0.GoToByWay(path, -1f);
                            }
                        }
                    }
                }
            }

            private bool IsAlreadyMovingToTarget(Vector3 targetPosition)
            {
                var pathController = BotOwner_0.Mover.ActualPathController;
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
}