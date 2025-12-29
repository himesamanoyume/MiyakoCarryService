
using System.Linq;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;

namespace MiyakoCarryService.Server.Generators.OrderQuestGeneration
{
    [Injectable]
    public class OrderQuestGenerator(
        OrderQuestHelper orderQuestHelper,
        RandomUtil randomUtil,
        OrderQuestRewardGenerator orderQuestRewardGenerator
    )
    {
        public RepeatableQuest Generate(
            MongoId sessionId,
            int carryServiceLevel,
            float discount,
            MongoId traderId,
            OrderConfig orderConfig)
        {
            var pickupConfig = orderConfig.OrderQuests.First().QuestConfig.Pickup;

            var quest = orderQuestHelper.GenerateOrderTemplate(
                RepeatableQuestType.Pickup,
                traderId,
                sessionId
            );

            var itemTypeToFetchWithCount = randomUtil.GetArrayValue(pickupConfig.ItemTypeToFetchWithMaxCount);

            var itemCountToFetch = carryServiceLevel switch
            {
                1 => randomUtil.RandInt(
                    (int)(itemTypeToFetchWithCount.MaximumPickupCount * discount * 0.18f), 
                    (int)(itemTypeToFetchWithCount.MaximumPickupCount * discount * 0.22f) + 1),
                2 => randomUtil.RandInt(
                    (int)(itemTypeToFetchWithCount.MaximumPickupCount * discount * 0.38f), 
                    (int)(itemTypeToFetchWithCount.MaximumPickupCount * discount * 0.42f) + 1),
                3 => randomUtil.RandInt(
                    (int)(itemTypeToFetchWithCount.MaximumPickupCount * discount * 0.58f), 
                    (int)(itemTypeToFetchWithCount.MaximumPickupCount * discount * 0.62f) + 1),
                4 => randomUtil.RandInt(
                    (int)(itemTypeToFetchWithCount.MaximumPickupCount * discount * 0.78f), 
                    (int)(itemTypeToFetchWithCount.MaximumPickupCount * discount * 0.82f) + 1),
                5 => randomUtil.RandInt(
                    (int)(itemTypeToFetchWithCount.MaximumPickupCount * discount * 0.98f), 
                    (int)(itemTypeToFetchWithCount.MaximumPickupCount * discount * 1.02f) + 1),
                _ => randomUtil.RandInt(
                    (int)(itemTypeToFetchWithCount.MaximumPickupCount * discount * 0.98f), 
                    (int)(itemTypeToFetchWithCount.MaximumPickupCount * discount * 1.02f) + 1)
            };

            var handoverItemCondition = quest.Conditions.AvailableForFinish.FirstOrDefault(x => x.ConditionType == "HandoverItem");
            handoverItemCondition.Target = new ListOrT<string>([itemTypeToFetchWithCount.ItemType], null);
            handoverItemCondition.Value = itemCountToFetch;

            quest.Rewards = orderQuestRewardGenerator.GenerateReward(1, traderId, orderConfig);

            return quest;
        }
    }
}