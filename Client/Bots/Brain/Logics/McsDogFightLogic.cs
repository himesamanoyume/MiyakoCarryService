using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    /// <summary>
    /// 近身走射（借鉴 SAIN DogFight）：走/撤与站定开火按时间戳交替，全部顺序 if 驱动（与 McsBrainLayer 风格一致）。
    /// 站定开火段与后撤段敌可见时侧身（原版 BotTilt ±5f 满幅），冲向敌人近身时有概率跳跃（借鉴 SAIN RushEnemyAction.checkJump）。
    /// 由 McsBrainLayer 在"路径距离近且刚见敌/刚被打"时进入。
    /// </summary>
    public class McsDogFightLogic : McsBotBaseLogic
    {
        /// <summary>
        /// 当前是否处于"站定开火"段（false = 走向敌人/后撤走位段）
        /// </summary>
        private bool _dogFightPhaseIsShooting = false;

        /// <summary>
        /// 当前段截止时间（Time.time），到期切换段
        /// </summary>
        private float _dogFightPhaseEndTime = 0f;

        /// <summary>
        /// 侧身方向：-1 左 / 1 右（进入时随机，换段时 50% 概率翻转）
        /// </summary>
        private int _dogFightTiltDirection = 1;

        /// <summary>
        /// 后撤目标点（切段时计算一次）
        /// </summary>
        private Vector3 _backupTarget = Vector3.zero;

        /// <summary>
        /// 下一次允许跳跃的时间（冲锋跳跃冷却）
        /// </summary>
        private float _nextJumpTime = 0f;

        /// <summary>
        /// 寻路刷新节流
        /// </summary>
        private float _nextPathUpdateTime = 0f;

        /// <summary>
        /// 走射速度倍率（SAIN STRAFE_SPEED=0.5：散步代替冲刺，走射观感）
        /// </summary>
        private const float STRAFE_SPEED = 0.5f;

        /// <summary>
        /// 敌人进入此距离（米）且可见时切站定开火段
        /// </summary>
        private const float ENEMY_CLOSE_SQUARE_DIST = 8f * 8f;

        /// <summary>
        /// 站定开火段时长下限（秒）：最短开火窗口，保证瞄准系统至少有一次完成机会
        /// </summary>
        private const float SHOOT_DURATION_MIN = 0.4f;

        /// <summary>
        /// 站定开火段时长上限兜底（秒）：Settings 不可用时使用
        /// </summary>
        private const float SHOOT_DURATION_MAX_FALLBACK = 0.8f;

        /// <summary>
        /// 后撤走位段时长范围（秒）
        /// </summary>
        private const float BACKUP_DURATION_MIN = 0.75f;
        private const float BACKUP_DURATION_MAX = 1.1f;

        /// <summary>
        /// 后撤距离与随机散布（米）（SAIN DogFight findBackupTarget：反方向 3m + 2m 随机）
        /// </summary>
        private const float BACKUP_DISTANCE = 3f;
        private const float BACKUP_RANDOM_RADIUS = 2f;

        /// <summary>
        /// 冲锋跳跃：敌距 1.5~6m 内有概率起跳；冷却 3s（SAIN checkJump）
        /// </summary>
        private const float JUMP_MIN_ENEMY_SQUARE_DIST = 1.5f * 1.5f;
        private const float JUMP_MAX_ENEMY_SQUARE_DIST = 6f * 6f;
        private const float JUMP_COOLDOWN = 3f;
        private const float JUMP_CHANCE = 35f;

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

            // 指令优先级（对齐老层语义）：驻守/跟随/保持阵位指令出现时立即停止自主走射缠斗，
            // 回正侧身并停止移动，交回层决策（层决策有 HoldPositionCommand/编队走位分支兜底）
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
                // 上一段是后撤（有后撤目标）则继续后撤，否则冲向敌人
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

            // 敌可见且近身 → 切站定开火段（时长上限从护航级别瞄准参数派生，随级别自适应）
            if (goalEnemy.IsVisible && botToEnemySqrDist <= ENEMY_CLOSE_SQUARE_DIST)
            {
                SwitchToShootingPhase(Random.Range(SHOOT_DURATION_MIN, CalcShootDurationMax()));
                BotOwner.StopMove();
                return;
            }

            if (_nextPathUpdateTime < Time.time)
            {
                _nextPathUpdateTime = Time.time + PATH_UPDATE_INTERVAL;
                BotOwner.GoToPoint(goalEnemy.Person.Position, true, -1f, false, false, true, false, false);
            }

            // 冲锋跳跃（借鉴 SAIN RushEnemyAction.checkJump）：近身冲刺途中概率起跳，TryJump 内部自检 CanJump
            if (goalEnemy.IsVisible
                && botToEnemySqrDist >= JUMP_MIN_ENEMY_SQUARE_DIST
                && botToEnemySqrDist <= JUMP_MAX_ENEMY_SQUARE_DIST
                && Time.time >= _nextJumpTime
                && MyExtensions.IsTrue100(JUMP_CHANCE))
            {
                _nextJumpTime = Time.time + JUMP_COOLDOWN;
                BotOwner.GetPlayer?.MovementContext?.TryJump();
            }

            if (goalEnemy.IsVisible)
            {
                // 敌可见：视线由 AimAndShootAtPoint 的 LookToPoint 接管，避免与移动朝向互相覆盖
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
                SwitchToMovingPhase(goalEnemy, Random.Range(BACKUP_DURATION_MIN, BACKUP_DURATION_MAX));
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
                // 敌可见：边撤边打，侧身+视线盯敌人
                TiltToSide(_dogFightTiltDirection);
                AimAndShootAtPoint(goalEnemy.GetPartToShoot());
            }
            else
            {
                BotOwner.Steering.LookToMovingDirection();
            }
        }

        /// <summary>
        /// 站定开火段时长上限：从护航级别瞄准参数派生（MAX_AIM_TIME 为引擎完成瞄准的基准上限，
        /// ×2 + 0.2s 保证站定段始终 ≥ 完成一次瞄准所需时间——低级别护航瞄准慢则站得更久），
        /// Settings 缺失时兜底 0.8f
        /// </summary>
        private float CalcShootDurationMax()
        {
            var aimingSettings = BotOwner?.Settings?.FileSettings?.Aiming;
            if (aimingSettings != null)
            {
                return aimingSettings.MAX_AIM_TIME * 2f + 0.2f;
            }

            return SHOOT_DURATION_MAX_FALLBACK;
        }

        /// <summary>
        /// 切入站定开火段：50% 概率翻转侧身方向
        /// </summary>
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

        /// <summary>
        /// 切入走位段：clearTarget 为 true 时清掉后撤点回到"冲向敌人"，false 时计算后撤点进入"后撤"
        /// </summary>
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

        /// <summary>
        /// 后撤目标点：敌人反方向 3m + 2m 随机散布，NavMesh 采样兜底（SAIN DogFight findBackupTarget 思路）
        /// </summary>
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
