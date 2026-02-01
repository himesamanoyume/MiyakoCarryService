
using System;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    internal sealed class LootingTargetLogic : McsBotBaseLogic
    {
        private int _currentLootingRetries = 0;
        private int _currentStuckRetries = 0;
        private float _lastTimeCheckDistance = 0f;
        private float _lastTimeDistance = -1f;
        public LootingTargetLogic(BotOwner botOwner) : base(botOwner)
        {

        }

        public override void Update(CustomLayer.ActionData data)
        {
            var mcsBotPlayerData = BotOwner.GetMcsBotData();
            if (mcsBotPlayerData.IsRunningCoroutine)
            {
                return;
            }

            mcsBotPlayerData.IsLooting = true;

            if (_lastTimeCheckDistance < Time.time)
            {
                _currentLootingRetries++;
                if (_currentLootingRetries > 30)
                {
                    mcsBotPlayerData.LootingTarget.IsNonNavigableItem = true;
                    mcsBotPlayerData.IsLooting = false;
                    _currentStuckRetries = 0;
                    _currentLootingRetries = 0;

                    return;
                }

                _lastTimeCheckDistance = Time.time + 2f;

                var targetPos = mcsBotPlayerData.LootingTarget.RootTransform.position;
                var offset = BotOwner.Position - targetPos;
                var distance = offset.sqrMagnitude;

                // 到达判定
                if (distance <= 1f && Math.Abs(offset.y) < 0.5f)
                {
                    BotOwner.SetTargetMoveSpeed(0f);
                    BotOwner.SetPose(0f);
                    BotOwner.Steering.LookToPoint(targetPos);
                    mcsBotPlayerData.StartLooting();
                    _currentStuckRetries = 0;
                    _lastTimeDistance = -1f; // 重置卡脚检测
                    return;
                }

                // 移动控制
                if (distance <= 5f)
                {
                    BotOwner.SetTargetMoveSpeed(1f);
                    BotOwner.SetPose(1f);
                    BotOwner.Steering.LookToMovingDirection();
                    BotOwner.Mover.Sprint(false);
                }

                if (_currentLootingRetries == 1)
                {
                    var pathStatus = BotOwner.GoToPoint(targetPos, mustHaveWay: true);
                    if (pathStatus != NavMeshPathStatus.PathComplete)
                    {
                        mcsBotPlayerData.LootingTarget.IsNonNavigableItem = true;
                        mcsBotPlayerData.IsLooting = false;
                        _currentStuckRetries = 0;
                        return;
                    }
                }

                if (_lastTimeDistance >= 0f && Math.Abs(_lastTimeDistance - distance) <= 0.3f)
                {
                    _currentStuckRetries++;
                }
                else
                {
                    _currentStuckRetries = 0; // 有移动就重置
                }

                if (_currentStuckRetries > 2)
                {
                    mcsBotPlayerData.LootingTarget.IsNonNavigableItem = true;
                    mcsBotPlayerData.IsLooting = false;
                    _currentStuckRetries = 0;
                    return;
                }

                _lastTimeDistance = distance;
            }
        }
    }
}