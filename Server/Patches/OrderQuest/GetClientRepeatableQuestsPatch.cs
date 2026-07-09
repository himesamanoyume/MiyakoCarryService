using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Patches.OrderQuest
{
    /// <summary>
    /// 在获取行动任务时，检查是否有新Order需要加入，随后将已有的Order任务一并返回给客户端
    /// </summary>
    public sealed class GetClientRepeatableQuestsPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RepeatableQuestController), nameof(RepeatableQuestController.GetClientRepeatableQuests));
        public static Dictionary<MongoId, Queue<RepeatableQuest>> QuestsQueueDict = new();

        private static ConfigController ConfigController { get => field ??= ServiceLocator.ServiceProvider.GetService<ConfigController>(); }
        private static Controllers.QuestController QuestController { get => field ??= ServiceLocator.ServiceProvider.GetService<Controllers.QuestController>(); }
        private static Controllers.ProfileController ProfileController { get => field ??= ServiceLocator.ServiceProvider.GetService<Controllers.ProfileController>(); }
        private static InfoController InfoController { get => field ??= ServiceLocator.ServiceProvider.GetService<InfoController>(); }
        private static ProfileHelper ProfileHelper { get => field ??= ServiceLocator.ServiceProvider.GetService<ProfileHelper>(); }
        private static TimeUtil TimeUtil { get => field ??= ServiceLocator.ServiceProvider.GetService<TimeUtil>(); }

        [PatchPostfix]
        public static void Postfix(MongoId sessionID, ref List<PmcDataRepeatableQuest> __result)
        {
            var currentTime = TimeUtil.GetTimeStamp();
            var fullProfile = ProfileHelper.GetFullProfile(sessionID);
            var pmcData = fullProfile.CharacterData.PmcData;
            var orderConfig = ConfigController.GetOrderConfig().OrderQuests.FirstOrDefault();
            var orderPendingPaymentTime = ConfigController.GetMcsPluginConfig().ServerConfig.OrderPendingPaymentTime;
            var generatedOrder = QuestController.GetRepeatableQuestSubTypeFromProfile(orderConfig, pmcData);

            if (QuestsQueueDict.TryGetValue(sessionID, out var orderQuestsQueue))
            {
                generatedOrder.EndTime = currentTime + orderPendingPaymentTime;
                while (orderQuestsQueue.Count > 0)
                {
                    var quest = orderQuestsQueue.Dequeue();
                    quest.Side = Enum.GetName(orderConfig.Side);
                    quest.ChangeCost.FirstOrDefault(x => x.TemplateId == ItemTpl.MONEY_ROUBLES).Count = (int)(currentTime + orderPendingPaymentTime);
                    generatedOrder.ActiveQuests.Add(quest);
                    generatedOrder.ChangeRequirement.Add(
                        quest.Id,
                        new ChangeRequirement
                        {
                            ChangeCost = quest.ChangeCost
                        }
                    );
                }
                QuestsQueueDict.Remove(sessionID);
            }

            QuestController.ProcessExpiredQuests(generatedOrder, pmcData);
            var mcsBotPlayerIds = InfoController.GetExpiredMcsBotPlayerIds();
            foreach (var kvp in mcsBotPlayerIds)
            {
                if (ProfileController.IsMcsBotPlayerInventoryMode(kvp.Key))
                {
                    continue;
                }
                InfoController.ProcessExpiredOrderInfo(kvp.Key);
                ProfileController.ProcessExpiredMcsBotPlayerProfiles(kvp.Key, kvp.Value);
            }

            if (currentTime < generatedOrder.EndTime - 1)
            {
                // Console.WriteLine("旧任务数据仍合法");
                __result.Add(generatedOrder);
                return;
            }

            generatedOrder.EndTime = currentTime + orderPendingPaymentTime;
            generatedOrder.InactiveQuests = [];
            generatedOrder.ChangeRequirement = [];

            __result.Add(
                new PmcDataRepeatableQuest
                {
                    Id = orderConfig.Id,
                    Name = generatedOrder.Name,
                    EndTime = generatedOrder.EndTime,
                    ActiveQuests = generatedOrder.ActiveQuests,
                    InactiveQuests = generatedOrder.InactiveQuests,
                    ChangeRequirement = generatedOrder.ChangeRequirement,
                    FreeChanges = generatedOrder.FreeChanges,
                    FreeChangesAvailable = generatedOrder.FreeChangesAvailable,
                }
            );
        }
    }
}