
using System.Linq;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Generators.OrderQuestGeneration
{
    [Injectable]
    public class MCSOrderQuestGenerator(
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
            int hours
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

            var order = Generate(sessionId, players, carryServiceLevel, discount, MCSTraderService.MiyakoTraderId, mcsConfigService.GetOrderConfig());
            return order;
        }

        private RepeatableQuest Generate(
            MongoId sessionId,
            int players,
            int carryServiceLevel,
            float discount,
            MongoId traderId,
            MCSOrderConfig orderConfig)
        {
            var completionConfig = orderConfig.OrderQuests.First().QuestConfig.CompletionConfig.First();

            var quest = mcsOrderQuestHelper.GenerateOrderTemplate(
                RepeatableQuestType.Completion,
                traderId,
                sessionId
            );

            var requestedItemCount = completionConfig.RequestedItemCount;
            var handoverItemCondition = quest.Conditions.AvailableForFinish.FirstOrDefault(x => x.ConditionType == "HandoverItem");
            handoverItemCondition.Value = 0;

            for (int i = 0; i < players; i++)
            {
                var currentRequestedItemCount = carryServiceLevel switch
                {
                    1 => randomUtil.RandInt(
                        (int)(requestedItemCount.Max * discount * 0.18f),
                        (int)(requestedItemCount.Max * discount * 0.22f) + 1),
                    2 => randomUtil.RandInt(
                        (int)(requestedItemCount.Min * discount * 0.38f),
                        (int)(requestedItemCount.Max * discount * 0.42f) + 1),
                    3 => randomUtil.RandInt(
                        (int)(requestedItemCount.Min * discount * 0.58f),
                        (int)(requestedItemCount.Max * discount * 0.62f) + 1),
                    4 => randomUtil.RandInt(
                        (int)(requestedItemCount.Min * discount * 0.78f),
                        (int)(requestedItemCount.Max * discount * 0.82f) + 1),
                    5 => randomUtil.RandInt(
                        (int)(requestedItemCount.Min * discount * 0.98f),
                        (int)(requestedItemCount.Max * discount * 1.02f) + 1),
                    _ => randomUtil.RandInt(
                        (int)(requestedItemCount.Min * discount * 0.98f),
                        (int)(requestedItemCount.Max * discount * 1.02f) + 1)
                };
                handoverItemCondition.Value += currentRequestedItemCount;
            }
            quest.Rewards = mcsOrderQuestRewardGenerator.GenerateReward(players, carryServiceLevel, traderId, orderConfig);
            return quest;
        }
    }
}