
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using MiyakoCarryService.Server.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Logger;
using MiyakoCarryService.Server.Models.Enums;

namespace MiyakoCarryService.Server.Generators.OrderQuestGeneration
{
    [Injectable]
    public class OrderQuestGenerator(
        SptLogger<OrderQuestGenerator> logger,
        RandomUtil randomUtil,
        OrderQuestRewardGenerator orderQuestRewardGenerator,
        ServerLocalisationService serverLocalisationService
    )
    {
        public RepeatableQuest GenerateOrderQuest(
            PmcData pmcData,
            int players,
            EBotType botType,
            int carryServiceLevel,
            int duration,
            CompletionConfig completionConfig,
            RepeatableQuest questTemplate
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
            logger.Info(serverLocalisationService.GetText(Locales.STARTINGORDERGENERATION));
            var order = Generate(players, botType, carryServiceLevel, duration, discount, Services.TraderService.MiyakoTraderId, completionConfig, questTemplate, miyakoTraderInfo.Standing.HasValue ? miyakoTraderInfo.Standing.Value : 0f);
            return order;
        }

        private RepeatableQuest Generate(
            int players,
            EBotType botType,
            int carryServiceLevel,
            int duration,
            float discount,
            MongoId traderId,
            CompletionConfig completionConfig,
            RepeatableQuest questTemplate,
            double traderStanding
        )
        {
            var requestedItemCount = completionConfig.RequestedItemCount;
            questTemplate.Conditions.AvailableForFinish = [];

            var isBossType = !Classification.BossTypes.Contains(botType);
            var additionMulti = isBossType ? 2f : 1f;
            var standingMulti = traderStanding < 0 ? 10.7f : 1f;

            for (int i = 0; i < players; i++)
            {
                var currentRequestedItemCount = randomUtil.RandInt(
                    (int)(requestedItemCount.Max * discount * (1 / carryServiceLevel - 0.02f) * additionMulti * standingMulti),
                    (int)(requestedItemCount.Max * discount * (1 / carryServiceLevel + 0.02f) * additionMulti * standingMulti) + 1
                    );

                var handoverItemCondition = new QuestCondition
                {
                    Id = new(),
                    Index = i,
                    ParentId = string.Empty,
                    DynamicLocale = true,
                    VisibilityConditions = [],
                    Target = new ListOrT<string>([new ("5449016a4bdc2d6f028b456f")], null),
                    Value = currentRequestedItemCount * duration,
                    MinDurability = 0,
                    MaxDurability = 100,
                    DogtagLevel = 0,
                    OnlyFoundInRaid = false,
                    IsEncoded = false,
                    ConditionType = "HandoverItem",
                };
                questTemplate.Conditions.AvailableForFinish.Add(handoverItemCondition);
            }
            questTemplate.Rewards = orderQuestRewardGenerator.GenerateReward(players, carryServiceLevel, traderId, traderStanding);
            return questTemplate;
        }
    }
}