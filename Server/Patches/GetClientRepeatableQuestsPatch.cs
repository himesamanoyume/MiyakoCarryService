using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Patches
{
    public sealed class GetClientRepeatableQuestsPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RepeatableQuestController), nameof(RepeatableQuestController.GetClientRepeatableQuests));
        public static Dictionary<MongoId, Queue<RepeatableQuest>> OrderQuestsQueueDict = new();

        [PatchPostfix]
        public static void Postfix(RepeatableQuestController __instance, MongoId sessionId, ref List<PmcDataRepeatableQuest> __result)
        {
            var mcsConfigService = ServiceLocator.ServiceProvider.GetService<MCSConfigService>();
            var mcsOrderQuestController = ServiceLocator.ServiceProvider.GetService<MCSOrderQuestController>();
            var mcsOrderInfoController = ServiceLocator.ServiceProvider.GetService<MCSOrderInfoController>();
            var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();
            var timeUtil = ServiceLocator.ServiceProvider.GetService<TimeUtil>();
            var currentTime = timeUtil.GetTimeStamp();
            var fullProfile = profileHelper.GetFullProfile(sessionId);
            var pmcData = fullProfile.CharacterData.PmcData;
            var orderConfig = mcsConfigService.GetOrderConfig().OrderQuests.First();
            var repeatableQuestControllerTraverse = Traverse.Create(__instance);
            var generatedOrder = repeatableQuestControllerTraverse.Method("GetRepeatableQuestSubTypeFromProfile", [orderConfig, pmcData]).GetValue<PmcDataRepeatableQuest>();

            if (OrderQuestsQueueDict.TryGetValue(sessionId, out var orderQuestsQueue))
            {
                generatedOrder.EndTime = currentTime + orderConfig.ResetTime;
                while (orderQuestsQueue.Count > 0)
                {
                    var quest = orderQuestsQueue.Dequeue();
                    Console.WriteLine("加入新任务");
                    quest.Side = Enum.GetName(orderConfig.Side);
                    quest.ChangeCost.FirstOrDefault(x => x.TemplateId == "5449016a4bdc2d6f028b456f").Count = (int?)(currentTime + orderConfig.ResetTime);
                    generatedOrder.ActiveQuests.Add(quest);
                    generatedOrder.ChangeRequirement.TryAdd(
                        quest.Id,
                        new ChangeRequirement
                        {
                            ChangeCost = quest.ChangeCost,
                            ChangeStandingCost = 0
                        }
                    );
                }
                OrderQuestsQueueDict.Remove(sessionId);
            }

            Console.WriteLine("将尝试清除过期订单、任务");
            mcsOrderQuestController.ProcessExpiredQuests(generatedOrder, pmcData);
            mcsOrderInfoController.ProcessExpiredOrderInfos(sessionId);

            if (currentTime < generatedOrder.EndTime - 1)
            {
                Console.WriteLine("旧任务数据仍合法");
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