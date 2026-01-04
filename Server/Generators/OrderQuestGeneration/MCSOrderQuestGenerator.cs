
using System.Linq;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;

namespace MiyakoCarryService.Server.Generators.OrderQuestGeneration
{
    [Injectable]
    public class MCSOrderQuestGenerator(
        ISptLogger<MCSOrderQuestGenerator> logger,
        RandomUtil randomUtil,
        MCSOrderQuestRewardGenerator mcsOrderQuestRewardGenerator,
        MCSOrderQuestHelper mcsOrderQuestHelper,
        MCSConfigService mcsConfigService
    )
    {
        public RepeatableQuest GenerateOrderQuest(
            MongoId sessionId,
            PmcData pmcData,
            int players,
            int carryServiceLevel,
            int duration
        )
        {
            var traderInfos = pmcData.TradersInfo;
            var miyakoTraderInfo = traderInfos[MCSTraderService.MiyakoTraderId];
            var loyaltyLevel = miyakoTraderInfo.LoyaltyLevel;
            var discount = loyaltyLevel switch
            {
                1 => 1.0f,
                2 => 0.98f,
                3 => 0.96f,
                4 => 0.94f,
                5 => 0.92f,
                _ => 1f,
            };
            logger.Info("开始生成订单");
            var order = Generate(sessionId, players, carryServiceLevel, duration, discount, MCSTraderService.MiyakoTraderId, mcsConfigService.GetOrderConfig());
            return order;
        }

        private RepeatableQuest Generate(
            MongoId sessionId,
            int players,
            int carryServiceLevel,
            int duration,
            float discount,
            MongoId traderId,
            MCSOrderConfig orderConfig
        )
        {
            var completionConfig = orderConfig.OrderQuests.First().QuestConfig.CompletionConfig.First();

            var quest = mcsOrderQuestHelper.GenerateOrderTemplate(
                RepeatableQuestType.Completion,
                traderId,
                sessionId
            );

            var requestedItemCount = completionConfig.RequestedItemCount;
            quest.Conditions.AvailableForFinish = [];

            for (int i = 0; i < players; i++)
            {
                var currentRequestedItemCount = carryServiceLevel switch
                {
                    1 => randomUtil.RandInt(
                        (int)(requestedItemCount.Max * discount * 0.18f),
                        (int)(requestedItemCount.Max * discount * 0.22f) + 1),
                    2 => randomUtil.RandInt(
                        (int)(requestedItemCount.Max * discount * 0.38f),
                        (int)(requestedItemCount.Max * discount * 0.42f) + 1),
                    3 => randomUtil.RandInt(
                        (int)(requestedItemCount.Max * discount * 0.58f),
                        (int)(requestedItemCount.Max * discount * 0.62f) + 1),
                    4 => randomUtil.RandInt(
                        (int)(requestedItemCount.Max * discount * 0.78f),
                        (int)(requestedItemCount.Max * discount * 0.82f) + 1),
                    5 => randomUtil.RandInt(
                        (int)(requestedItemCount.Max * discount * 0.98f),
                        (int)(requestedItemCount.Max * discount * 1.02f) + 1),
                    _ => randomUtil.RandInt(
                        (int)(requestedItemCount.Max * discount * 0.98f),
                        (int)(requestedItemCount.Max * discount * 1.02f) + 1)
                };

                var handoverItemCondition = new QuestCondition
                {
                    Id = new MongoId(),
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
                quest.Conditions.AvailableForFinish.Add(handoverItemCondition);
            }
            quest.Rewards = mcsOrderQuestRewardGenerator.GenerateReward(players, carryServiceLevel, traderId);
            logger.Info("订单任务信息构建结束");
            return quest;
        }
    }
}