
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class BlindFireBlockedLogic : McsBotBaseLogic
    {
        private float _nextAimUpdateTime = 0f;
        private Vector3 _suppressTargetPos = Vector3.zero;
        private int _tiltDirection = 1;
        private const float AIM_JITTER_DISTANCE = 0.5f;

        public BlindFireBlockedLogic(BotOwner botOwner) : base(botOwner)
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy?.Person != null)
            {
                _tiltDirection = CalcAvoidTiltDirection(goalEnemy.Person.Position);
            }
            else
            {
                _tiltDirection = MyExtensions.RandomSing();
            }
        }

        public override void Start()
        {
            base.Start();
            SetLeftStanceShoulder(_tiltDirection == -1, _tiltDirection);
        }

        public override void Stop()
        {
            base.Stop();
            SetLeftStanceShoulder(false, 0);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy == null)
            {
                return;
            }

            BotOwner.Sprint(false, false);
            BotOwner.SetPose(1f);
            SetLeftStanceShoulder(_tiltDirection == -1, _tiltDirection);

            if (_nextAimUpdateTime < Time.time)
            {
                _nextAimUpdateTime = Time.time + 1.5f;
                _suppressTargetPos = goalEnemy.EnemyLastPosition + new Vector3(
                    Random.Range(-AIM_JITTER_DISTANCE, AIM_JITTER_DISTANCE),
                    Random.Range(0f, AIM_JITTER_DISTANCE),
                    Random.Range(-AIM_JITTER_DISTANCE, AIM_JITTER_DISTANCE));
            }

            AimAndShootAtPoint(_suppressTargetPos, false);
        }
    }
}
