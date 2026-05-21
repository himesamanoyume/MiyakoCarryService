
using System;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class GoToLootTargetLogic : McsBotBaseLogic
    {
        private int _currentLootingRetries = 0;
        private float _lastTimeCheckDistance = 0f;
        private float _lastTimeDistance = -1f;
        public GoToLootTargetLogic(BotOwner botOwner) : base(botOwner)
        {

        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
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
                    MiyakoCarryServicePlugin.Logger.LogWarning("重试超时");
                    mcsBotPlayerData.IsLooting = false;
                    _currentLootingRetries = 0;
                    return;
                }

                _lastTimeCheckDistance = Time.time + 2f;

                var lootPos = mcsBotPlayerData.LootingTarget.RootTransform.position;
                var offset = BotOwner.Position - lootPos;
                var distance = BotOwner.Position.McsSqrDistance(lootPos);

                Tools.BetterDestination(3f, lootPos, out var targetPos);

                MiyakoCarryServicePlugin.Logger.LogWarning($"{mcsBotPlayerData.Player.Profile.Nickname}, 目标: {mcsBotPlayerData.LootingTarget.Item.Name.McsLocalized()}, 价值: {mcsBotPlayerData.LootingTarget.Offer.Price}, 坐标: {targetPos}, 距离: {distance}");

                // 到达判定
                if (distance <= 9f && Math.Abs(offset.y) < 2f)
                {
                    BotOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.OnLoot,
                        Key = mcsBotPlayerData.LootingTarget.Item.Name
                    });
                    BotOwner.SetTargetMoveSpeed(0f);
                    BotOwner.SetPose(0f);
                    BotOwner.Steering.LookToPoint(lootPos);
                    mcsBotPlayerData.StartLooting();
                    _lastTimeDistance = Mathf.Infinity; // 重置卡脚检测
                    return;
                }

                // 移动控制
                if (distance <= 5f)
                {
                    BotOwner.SetTargetMoveSpeed(1f);
                    BotOwner.Steering.LookToMovingDirection();
                    BotOwner.Mover.Sprint(false);
                }

                var pathStatus = BotOwner.GoToPoint(targetPos, mustHaveWay: true);
                if (pathStatus != NavMeshPathStatus.PathComplete)
                {
                    MiyakoCarryServicePlugin.Logger.LogWarning("没有路径");
                    mcsBotPlayerData.IsLooting = false;
                    return;
                }

                if (_lastTimeDistance > 0f)
                {
                    BotOwner.CheckStuck();
                }

                _lastTimeDistance = distance;
            }
        }
    }
}