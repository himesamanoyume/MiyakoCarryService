using DrakiaXYZ.BigBrain.Brains;
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    /// <summary>
    /// 掩体内探头射击节奏 + 盲射（借鉴 SAIN SeekCoverAction 探头循环与 BlindFireController）：
    /// 探头/缩回按时间戳交替，全部顺序 if 驱动（与 McsBrainLayer 风格一致）。
    /// 敌可见 → 保持侧身（原版 BotTilt ±5f 满幅）持续射击；敌不可见 → 侧身+盲射一段 → 缩回随机时长 → 换边再探头。
    /// 由 McsBrainLayer 在"Memory.IsInCover 且能对敌射击"时进入。
    /// </summary>
    public class McsCoverPeakLogic : McsBotBaseLogic
    {
        /// <summary>
        /// 缩回截止时间：>0 表示正在缩回（不开火），<=0 表示探头中
        /// </summary>
        private float _peakRetractUntil = 0f;

        /// <summary>
        /// 当前段截止时间（探头段的盲射时长 / 缩回段的时长）
        /// </summary>
        private float _peakPhaseEndTime = 0f;

        /// <summary>
        /// 探头方向：-1 左 / 1 右（进入时随机，缩回到期换边）
        /// </summary>
        private int _peakDirection = 1;

        /// <summary>
        /// 盲射瞄准点刷新节流
        /// </summary>
        private float _nextAimUpdateTime = 0f;
        private Vector3 _blindFireTargetPos = Vector3.zero;

        /// <summary>
        /// 探头段时长范围（秒）
        /// </summary>
        private const float PEEK_DURATION_MIN = 1.2f;
        private const float PEEK_DURATION_MAX = 2.5f;

        /// <summary>
        /// 缩回段时长范围（秒）
        /// </summary>
        private const float RETRACT_DURATION_MIN = 0.75f;
        private const float RETRACT_DURATION_MAX = 2f;

        /// <summary>
        /// 盲射瞄准点刷新间隔与抖动（米）
        /// </summary>
        private const float BLIND_AIM_UPDATE_INTERVAL = 1.5f;
        private const float BLIND_AIM_JITTER_DISTANCE = 0.5f;

        public McsCoverPeakLogic(BotOwner botOwner) : base(botOwner)
        {
        }

        public override void Start()
        {
            base.Start();
            _peakRetractUntil = 0f;
            _peakPhaseEndTime = Time.time + Random.Range(PEEK_DURATION_MIN, PEEK_DURATION_MAX);
            _peakDirection = Random.value < 0.5f ? -1 : 1;
            _nextAimUpdateTime = 0f;
        }

        public override void Stop()
        {
            TiltToSide(0);
            SetBlindFire(0f);
            base.Stop();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy == null)
            {
                return;
            }

            BotOwner.Sprint(false, false);

            if (_peakRetractUntil > 0f)
            {
                UpdateRetractedPhase(goalEnemy);
                return;
            }

            UpdatePeekPhase(goalEnemy);
        }

        /// <summary>
        /// 探头段：敌可见 → 侧身连射；敌不可见 → 侧身+盲射朝最后已知点
        /// </summary>
        private void UpdatePeekPhase(EnemyInfo goalEnemy)
        {
            if (Time.time > _peakPhaseEndTime)
            {
                // 探头到期 → 缩回
                _peakRetractUntil = Time.time + Random.Range(RETRACT_DURATION_MIN, RETRACT_DURATION_MAX);
                _peakPhaseEndTime = _peakRetractUntil;
                TiltToSide(0);
                SetBlindFire(0f);
                return;
            }

            TiltToSide(_peakDirection);

            if (goalEnemy.IsVisible)
            {
                SetBlindFire(0f);
                AimAndShootAtPoint(goalEnemy.GetPartToShoot());
                return;
            }

            // 敌不可见：盲射姿态 + 朝敌人最后已知位置开火
            SetBlindFire(_peakDirection);

            if (_nextAimUpdateTime < Time.time)
            {
                _nextAimUpdateTime = Time.time + BLIND_AIM_UPDATE_INTERVAL;
                _blindFireTargetPos = goalEnemy.EnemyLastPosition + new Vector3(
                    Random.Range(-BLIND_AIM_JITTER_DISTANCE, BLIND_AIM_JITTER_DISTANCE),
                    Random.Range(0f, BLIND_AIM_JITTER_DISTANCE),
                    Random.Range(-BLIND_AIM_JITTER_DISTANCE, BLIND_AIM_JITTER_DISTANCE));
            }

            // 盲射：有意朝敌人最后已知位置压制（允许糊墙，压制语义），跳过 CanShoot 弹道门控
            AimAndShootAtPoint(_blindFireTargetPos, false);
        }

        /// <summary>
        /// 缩回段：回正不开火，到期换边重新探头；敌重新可见时立即切回探头
        /// </summary>
        private void UpdateRetractedPhase(EnemyInfo goalEnemy)
        {
            if (goalEnemy.IsVisible)
            {
                // 敌重新可见：立即结束缩回回到探头连射
                EnterPeekPhase();
                return;
            }

            if (Time.time > _peakPhaseEndTime)
            {
                // 缩回到期 → 换边探头
                _peakDirection = -_peakDirection;
                EnterPeekPhase();
                return;
            }

            TiltToSide(0);
            SetBlindFire(0f);
        }

        private void EnterPeekPhase()
        {
            _peakRetractUntil = 0f;
            _peakPhaseEndTime = Time.time + Random.Range(PEEK_DURATION_MIN, PEEK_DURATION_MAX);
        }

        private void SetBlindFire(float blind)
        {
            var movementContext = BotOwner.GetPlayer?.MovementContext;
            if (movementContext != null)
            {
                movementContext.SetBlindFire(blind);
            }
        }
    }
}
