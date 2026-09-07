using DrakiaXYZ.BigBrain.Brains;
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    /// <summary>
    /// 压制射击（借鉴 SAIN 反压制 TrySuppressAnyEnemy）：
    /// 敌人不可见但近期目击过时，朝敌人最后已知位置开火，用于掩护与压制，替代无脑冲脸。
    /// 由 McsTestBrainLayer 在"敌不可见且失联窗口内"进入。
    /// </summary>
    public class McsSuppressFireLogic : McsBotBaseLogic
    {
        private float _nextAimUpdateTime = 0f;
        private Vector3 _suppressTargetPos = Vector3.zero;

        private const float AIM_UPDATE_INTERVAL = 1.5f;
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
                _nextAimUpdateTime = Time.time + AIM_UPDATE_INTERVAL;
                _suppressTargetPos = goalEnemy.EnemyLastPosition + new Vector3(
                    UnityEngine.Random.Range(-AIM_JITTER_DISTANCE, AIM_JITTER_DISTANCE),
                    UnityEngine.Random.Range(0f, AIM_JITTER_DISTANCE),
                    UnityEngine.Random.Range(-AIM_JITTER_DISTANCE, AIM_JITTER_DISTANCE));
            }

            AimAndShootAtPoint(_suppressTargetPos);
        }
    }
}
