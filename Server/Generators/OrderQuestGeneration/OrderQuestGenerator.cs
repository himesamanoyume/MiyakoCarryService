
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;

namespace MiyakoCarryService.Server.Generators.OrderQuestGeneration
{
    [Injectable]
    public class OrderQuestGenerator(
        RandomUtil randomUtil,
        OrderQuestRewardGenerator orderQuestRewardGenerator
    )
    {
        public RepeatableQuest GenerateOrderQuest(
            PmcData pmcData,
            int players,
            SpawnType spawnType,
            int carryServiceLevel,
            int duration,
            CompletionConfig completionConfig,
            RepeatableQuest questTemplate,
            double punishmentMulti
        )
        {
            var traderInfos = pmcData.TradersInfo;
            traderInfos.TryGetValue(Services.TraderService.MiyakoTraderId, out var miyakoTraderInfo);
            var loyaltyLevel = miyakoTraderInfo is null ? 1 : miyakoTraderInfo.LoyaltyLevel;
            var discount = loyaltyLevel switch
            {
                1 => 1.0f,
                2 => 0.98f,
                3 => 0.96f,
                4 => 0.94f,
                5 => 0.92f,
                _ => 1f,
            };
            var order = Generate(players, spawnType, carryServiceLevel, duration, discount, Services.TraderService.MiyakoTraderId, completionConfig, questTemplate, punishmentMulti);
            return order;
        }

        private RepeatableQuest Generate(
            int players,
            SpawnType spawnType,
            int carryServiceLevel,
            int duration,
            float discount,
            MongoId traderId,
            CompletionConfig completionConfig,
            RepeatableQuest questTemplate,
            double punishmentMulti
        )
        {
            var requestedItemCount = completionConfig.RequestedItemCount;
            questTemplate.Conditions.AvailableForFinish = [];

            var additionMulti = spawnType.IsBoss ? 2f : 1f;

            for (int i = 0; i < players; i++)
            {
                var currentRequestedItemCount = randomUtil.RandInt(
                    (int)(requestedItemCount.Max * discount * (0.75f + 0.05f * carryServiceLevel - 0.02f) * additionMulti * (1 + punishmentMulti)),
                    (int)(requestedItemCount.Max * discount * (0.75f + 0.05f * carryServiceLevel + 0.02f) * additionMulti * (1 + punishmentMulti)) + 1
                    );

                var handoverItemCondition = new QuestCondition
                {
                    Id = new(),
                    Index = i,
                    ParentId = string.Empty,
                    DynamicLocale = true,
                    VisibilityConditions = [],
                    Target = new ListOrT<string>([new (ItemTpl.MONEY_ROUBLES)], null),
                    Value = currentRequestedItemCount * duration,
                    OnlyFoundInRaid = false,
                    IsEncoded = false,
                    ConditionType = "HandoverItem",
                };
                questTemplate.Conditions.AvailableForFinish.Add(handoverItemCondition);
            }
            questTemplate.Rewards = orderQuestRewardGenerator.GenerateReward(players, carryServiceLevel, traderId);
            return questTemplate;
        }
    }
}