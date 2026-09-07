using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    /// <summary>
    /// 站桩射击 + 随机微走位（借鉴 SAIN StandAndShootAction 的 moveShoot）：
    /// 基础行为沿用原版 ShootFromPlace，站立期间按概率朝敌人轴向随机旋转 70°~110° 的 6m 侧移点走射，
    /// 到位后回到原版站桩。"露头换一个角度打"，破除机械站桩。
    /// </summary>
    public class McsStandAndShootLogic : McsBotBaseLogic
    {
        private ShootFromPlace _baseLogic;
        private float _nextStrafeCheckTime = 0f;
        private float _strafeUntilTime = 0f;
        private Vector3 _strafeTarget = Vector3.zero;

        private const float STRAFE_CHANCE = 40f;
        private const float STRAFE_CHECK_INTERVAL_MIN = 2.5f;
        private const float STRAFE_CHECK_INTERVAL_MAX = 5f;
        private const float STRAFE_DISTANCE = 6f;
        private const float STRAFE_DURATION = 2.5f;
        private const float STRAFE_MIN_ROTATION = 70f;
        private const float STRAFE_MAX_ROTATION = 110f;
        private const float STRAFE_ARRIVE_SQUARE_DIST = 0.75f * 0.75f;

        public McsStandAndShootLogic(BotOwner botOwner) : base(botOwner)
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

            // 指令优先级（对齐老层语义）：驻守/跟随/保持阵位指令下禁止一切位移——已激活的走位段立即终止，
            // 也不再寻找新走位点，退化为纯站桩（原版 ShootFromPlace 零位移）
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
                _nextStrafeCheckTime = Time.time + UnityEngine.Random.Range(STRAFE_CHECK_INTERVAL_MIN, STRAFE_CHECK_INTERVAL_MAX);
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

            if (BotOwner.Position.McsSqrDistance(_strafeTarget) <= STRAFE_ARRIVE_SQUARE_DIST)
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
            var rotationAngle = Random.Range(STRAFE_MIN_ROTATION, STRAFE_MAX_ROTATION) * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
            var strafeDirection = Quaternion.Euler(0f, rotationAngle, 0f) * directionToEnemy;
            var rawTarget = BotOwner.Position + strafeDirection * STRAFE_DISTANCE;
            rawTarget.y = BotOwner.Position.y;

            // 被障碍物截断时取射线命中点，避免穿墙目标
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
