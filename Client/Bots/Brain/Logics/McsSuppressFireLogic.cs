using DrakiaXYZ.BigBrain.Brains;
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    /// <summary>
    /// 重点参考了SAIN
    /// </summary>
    public class McsSuppressFireLogic : McsBotBaseLogic
    {
        private float _nextAimUpdateTime = 0f;
        private Vector3 _suppressTargetPos = Vector3.zero;
        private const float AIM_JITTER_DISTANCE = 0.5f;

        public McsSuppressFireLogic(BotOwner botOwner) : base(botOwner)
        {
            
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

            if (_nextAimUpdateTime < Time.time)
            {
                _nextAimUpdateTime = Time.time + 1.5f;
                _suppressTargetPos = goalEnemy.EnemyLastPosition + new Vector3(
                    UnityEngine.Random.Range(-AIM_JITTER_DISTANCE, AIM_JITTER_DISTANCE),
                    UnityEngine.Random.Range(0f, AIM_JITTER_DISTANCE),
                    UnityEngine.Random.Range(-AIM_JITTER_DISTANCE, AIM_JITTER_DISTANCE));
            }

            AimAndShootAtPoint(_suppressTargetPos, false);
        }
    }
}