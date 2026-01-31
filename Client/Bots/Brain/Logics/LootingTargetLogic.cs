
using System;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Datas;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    internal sealed class LootingTargetLogic : McsBotBaseLogic
    {
        private int _currentLootingRetries = 0;
        private int _currentStuckRetries = 0;
        private float _lastTimeCheckDistance = 0f;
        private float _lastTimeDistance = 0f;
        public LootingTargetLogic(BotOwner botOwner) : base(botOwner)
        {

        }

        public override void Update(CustomLayer.ActionData data)
        {
            var lootingData = data as LootingData;
            lootingData.McsBotPlayerData.IsLooting = true;
            if (_lastTimeCheckDistance < Time.time)
            {
                _currentLootingRetries++;
                _lastTimeCheckDistance = Time.time + 2f;

                var targetPos = lootingData.McsBotPlayerData.LootingTarget.Transform.position;

                var offset = BotOwner.Position - targetPos;
                var distance = offset.sqrMagnitude;

                if (distance > 1f && distance <= 5f)
                {
                    BotOwner.SetTargetMoveSpeed(1f);
                    BotOwner.SetPose(1f);
                    BotOwner.Steering.LookToMovingDirection();
                }
                else if (distance <= 1f && Math.Abs(offset.y) < 0.5f)
                {
                    BotOwner.SetTargetMoveSpeed(0f);
                    BotOwner.SetPose(0f);
                    BotOwner.Steering.LookToPoint(targetPos);
                    // 开始掠夺
                    
                    // 
                    return;
                }

                if (distance <= 5f)
                {
                    BotOwner.Mover.Sprint(false);
                }

                if (_currentLootingRetries == 1)
                {
                    var pathStatus = BotOwner.GoToPoint(targetPos, mustHaveWay: true);
                    if (pathStatus != NavMeshPathStatus.PathComplete)
                    {
                        lootingData.McsBotPlayerData.LootingTarget.IsNonNavigableItem = false;
                        lootingData.McsBotPlayerData.IsLooting = false;
                        _currentStuckRetries = 0;
                        return;
                    }
                }
                
                if (Math.Abs(_lastTimeDistance - distance) <= 0.3f)
                {
                    _currentStuckRetries++;
                }

                if (_currentStuckRetries > 2)
                {
                    lootingData.McsBotPlayerData.LootingTarget.IsNonNavigableItem = false;
                    lootingData.McsBotPlayerData.IsLooting = false;
                    _currentStuckRetries = 0;
                    return;
                }

                _lastTimeDistance = offset.sqrMagnitude;
            }
        }
    }
}