using System;
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

namespace MiyakoCarryService.Server.Generators.OrderQuestGeneration
{
    [Injectable]
    public class OrderQuestRewardGenerator(
        RepeatableQuestRewardGenerator repeatableQuestRewardGenerator
    )
    {
        public Dictionary<string, List<Reward>> GenerateReward(
            int carryServiceLevel,
            MongoId traderId,
            OrderConfig orderConfig
        )
        {
            var repeatableQuestRewardGeneratorTraverse = Traverse.Create(repeatableQuestRewardGenerator);
            var rewardParams = repeatableQuestRewardGeneratorTraverse.Method("GetQuestRewardValues", [orderConfig.OrderQuests.First().RewardScaling, carryServiceLevel, 1]).GetValue<QuestRewardValues>();

            var rewards = new Dictionary<string, List<Reward>>
            {
                { "Success", [] },
                { "Started", [] },
                { "Fail", [] },
            };

            var rewardIndex = -1;

            if (rewardParams.RewardReputation > 0)
            {
                Reward reward = new()
                {
                    Id = new MongoId(),
                    Unknown = false,
                    GameMode = [],
                    AvailableInGameEditions = [],
                    Target = traderId,
                    Value = rewardParams.RewardReputation,
                    Type = RewardType.TraderStanding,
                    Index = rewardIndex,
                };
                rewards["Success"].Add(reward);
                rewardIndex++;
            }

            return rewards;
        }
    }
}