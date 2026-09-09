using DrakiaXYZ.BigBrain.Brains;
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    /// <summary>
    /// 重点参考了SAIN
    /// </summary>
    public class McsCoverPeakLogic : McsBotBaseLogic
    {
        private float _peakRetractUntil = 0f;
        private float _peakPhaseEndTime = 0f;
        private int _peakDirection = 1;
        private float _nextAimUpdateTime = 0f;
        private Vector3 _blindFireTargetPos = Vector3.zero;
        private const float PEEK_DURATION_MIN = 1.2f;
        private const float PEEK_DURATION_MAX = 2.5f;
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

        private void UpdatePeekPhase(EnemyInfo goalEnemy)
        {
            if (Time.time > _peakPhaseEndTime)
            {
                _peakRetractUntil = Time.time + Random.Range(0.75f, 2f);
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

            SetBlindFire(_peakDirection);

            if (_nextAimUpdateTime < Time.time)
            {
                _nextAimUpdateTime = Time.time + 1.5f;
                _blindFireTargetPos = goalEnemy.EnemyLastPosition + new Vector3(
                    Random.Range(-BLIND_AIM_JITTER_DISTANCE, BLIND_AIM_JITTER_DISTANCE),
                    Random.Range(0f, BLIND_AIM_JITTER_DISTANCE),
                    Random.Range(-BLIND_AIM_JITTER_DISTANCE, BLIND_AIM_JITTER_DISTANCE));
            }

            AimAndShootAtPoint(_blindFireTargetPos, false);
        }

        private void UpdateRetractedPhase(EnemyInfo goalEnemy)
        {
            if (goalEnemy.IsVisible)
            {
                EnterPeekPhase();
                return;
            }

            if (Time.time > _peakPhaseEndTime)
            {
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