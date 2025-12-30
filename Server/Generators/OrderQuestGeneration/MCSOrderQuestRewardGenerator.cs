using System.Collections.Generic;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;

namespace MiyakoCarryService.Server.Generators.OrderQuestGeneration
{
    [Injectable]
    public class MCSOrderQuestRewardGenerator()
    {
        public Dictionary<string, List<Reward>> GenerateReward(
            int players,
            int carryServiceLevel,
            MongoId traderId
        )
        {
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
                reward.Value += 0.01 * carryServiceLevel;
            }

            return rewards;
        }
    }
}