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
        public static Dictionary<MongoId, Queue<RepeatableQuest>> OrderQuestsQueueDict = new();

        [PatchPostfix]
        public static void Postfix(MongoId sessionID, ref List<PmcDataRepeatableQuest> __result)
        {
            var configController = ServiceLocator.ServiceProvider.GetService<ConfigController>();
            var orderQuestController = ServiceLocator.ServiceProvider.GetService<OrderQuestController>();
            var profileController = ServiceLocator.ServiceProvider.GetService<Controllers.ProfileController>();
            var orderInfoController = ServiceLocator.ServiceProvider.GetService<OrderInfoController>();
            var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();
            var timeUtil = ServiceLocator.ServiceProvider.GetService<TimeUtil>();
            var currentTime = timeUtil.GetTimeStamp();
            var fullProfile = profileHelper.GetFullProfile(sessionID);
            var pmcData = fullProfile.CharacterData.PmcData;
            var orderConfig = configController.GetOrderConfig().OrderQuests.First();
            var generatedOrder = orderQuestController.GetRepeatableQuestSubTypeFromProfile(orderConfig, pmcData);

            if (OrderQuestsQueueDict.TryGetValue(sessionID, out var orderQuestsQueue))
            {
                generatedOrder.EndTime = currentTime + orderConfig.ResetTime;
                while (orderQuestsQueue.Count > 0)
                {
                    var quest = orderQuestsQueue.Dequeue();
                    // Console.WriteLine("加入新任务");
                    quest.Side = Enum.GetName(orderConfig.Side);
                    quest.ChangeCost.FirstOrDefault(x => x.TemplateId == ItemTpl.MONEY_ROUBLES).Count = (int)(currentTime + orderConfig.ResetTime);
                    generatedOrder.ActiveQuests.Add(quest);
                    generatedOrder.ChangeRequirement.Add(
                        quest.Id,
                        new ChangeRequirement
                        {
                            ChangeCost = quest.ChangeCost
                        }
                    );
                }
                OrderQuestsQueueDict.Remove(sessionID);
            }

            // Console.WriteLine("将尝试清除过期订单、任务");
            orderQuestController.ProcessExpiredQuests(generatedOrder, pmcData);
            var mcsBotPlayerIds = orderInfoController.GetExpiredMcsBotPlayerIds();
            foreach (var kvp in mcsBotPlayerIds)
            {
                if (profileController.IsMcsBotPlayerInventoryMode(kvp.Key))
                {
                    continue;
                }
                profileController.ProcessExpiredMcsBotPlayerProfiles(kvp.Key, kvp.Value);
            }

            if (currentTime < generatedOrder.EndTime - 1)
            {
                // Console.WriteLine("旧任务数据仍合法");
                __result.Add(generatedOrder);
                return;
            }

            generatedOrder.EndTime = currentTime + orderConfig.ResetTime;
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