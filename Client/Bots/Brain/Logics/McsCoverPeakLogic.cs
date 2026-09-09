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
        private float _peakDirectionFlipUntil = 0f;
        private bool _isBlindFiring = false;
        private float _lastBlindShootTime = 0f;
        private float _blindFireEndTime = 0f;
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
            _peakDirectionFlipUntil = 0f;
            _isBlindFiring = false;
            _lastBlindShootTime = 0f;
            _blindFireEndTime = 0f;
            _nextAimUpdateTime = 0f;
        }

        public override void Stop()
        {
            TiltToSide(0);
            SetBlindFire(0);
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
            if (goalEnemy.IsVisible && goalEnemy.CanShoot)
            {
                if (_isBlindFiring)
                {
                    _isBlindFiring = false;
                    SetBlindFire(0);
                }
                _peakPhaseEndTime = Time.time + Random.Range(PEEK_DURATION_MIN, PEEK_DURATION_MAX);
                TiltToSide(_peakDirection);
                AimAndShootAtPoint(goalEnemy.GetPartToShoot());
                return;
            }

            if (_isBlindFiring)
            {
                var time = Time.time;
                if (time - _lastBlindShootTime > 0.8f || time > _blindFireEndTime)
                {
                    _isBlindFiring = false;
                    SetBlindFire(0);
                    TiltToSide(0);
                    EnterRetractedPhase();
                    return;
                }

                SetBlindFire(_peakDirection);
                TiltToSide(_peakDirection);
                if (_nextAimUpdateTime < time)
                {
                    RefreshBlindFireTarget();
                }
                if (AimAndShootAtPoint(_blindFireTargetPos, false))
                {
                    _lastBlindShootTime = time;
                }
                return;
            }

            if (Time.time > _peakPhaseEndTime)
            {
                StartBlindFire();
                return;
            }

            TiltToSide(_peakDirection);
            SetBlindFire(0);
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
                if (Time.time >= _peakDirectionFlipUntil)
                {
                    _peakDirection = -_peakDirection;
                    _peakDirectionFlipUntil = Time.time + 2f;
                }
                EnterPeekPhase();
                return;
            }

            TiltToSide(0);
            SetBlindFire(0);
        }

        private void EnterPeekPhase()
        {
            _peakRetractUntil = 0f;
            _peakPhaseEndTime = Time.time + Random.Range(PEEK_DURATION_MIN, PEEK_DURATION_MAX);
        }

        private void StartBlindFire()
        {
            _isBlindFiring = true;
            var time = Time.time;
            _lastBlindShootTime = time;
            _blindFireEndTime = time + 5f;
            SetBlindFire(_peakDirection);
            RefreshBlindFireTarget();
        }

        private void EnterRetractedPhase()
        {
            _peakRetractUntil = Time.time + Random.Range(1.2f, 2f);
            _peakPhaseEndTime = _peakRetractUntil;
        }

        private void RefreshBlindFireTarget()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy == null)
            {
                return;
            }

            _nextAimUpdateTime = Time.time + 1.5f;
            _blindFireTargetPos = goalEnemy.EnemyLastPosition + new Vector3(
                Random.Range(-BLIND_AIM_JITTER_DISTANCE, BLIND_AIM_JITTER_DISTANCE),
                Random.Range(0f, BLIND_AIM_JITTER_DISTANCE),
                Random.Range(-BLIND_AIM_JITTER_DISTANCE, BLIND_AIM_JITTER_DISTANCE));
        }

        private void SetBlindFire(int blind)
        {
            var movementContext = BotOwner.GetPlayer?.MovementContext;
            if (movementContext != null)
            {
                movementContext.SetBlindFire(blind);
            }
        }
    }
}