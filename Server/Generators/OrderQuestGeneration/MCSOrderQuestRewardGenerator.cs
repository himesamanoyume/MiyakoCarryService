using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators.RepeatableQuestGeneration;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Repeatable;
using SPTarkov.Server.Core.Models.Utils;

namespace MiyakoCarryService.Server.Generators.OrderQuestGeneration
{
    [Injectable]
    public class MCSOrderQuestRewardGenerator(
        ISptLogger<MCSOrderQuestRewardGenerator> logger,
        RepeatableQuestRewardGenerator repeatableQuestRewardGenerator
    )
    {
        public Dictionary<string, List<Reward>> GenerateReward(
            int players,
            int carryServiceLevel,
            MongoId traderId,
            MCSOrderConfig orderConfig
        )
        {
            var repeatableQuestRewardGeneratorTraverse = Traverse.Create(repeatableQuestRewardGenerator);

            var rewards = new Dictionary<string, List<Reward>>
            {
                { "Success", [] },
                { "Started", [] },
                { "Fail", [] },
            };

            Reward reward = new()
            {
                Id = new MongoId(),
                Unknown = false,
                GameMode = [],
                AvailableInGameEditions = [],
                Target = traderId,
                Value = 0,
                Type = RewardType.TraderStanding,
                Index = 0,
            };
            
            rewards["Success"].Add(reward);

            for (int i = 0; i < players; i++)
            {
                var rewardParams = repeatableQuestRewardGeneratorTraverse.Method("GetQuestRewardValues", [orderConfig.OrderQuests.First().RewardScaling, carryServiceLevel, 1]).GetValue<QuestRewardValues>();
                logger.Info($"生成声望: {rewardParams.RewardReputation}");
                if (rewardParams.RewardReputation > 0)
                {
                    reward.Value += rewardParams.RewardReputation;
                }
            }

            return rewards;
        }
    }
}