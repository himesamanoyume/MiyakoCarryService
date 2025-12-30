using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
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
        public static void Postfix(RepeatableQuestController __instance, MongoId sessionID, ref List<PmcDataRepeatableQuest> __result)
        {
            var mcsConfigService = ServiceLocator.ServiceProvider.GetService<MCSConfigService>();
            var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();
            var timeUtil = ServiceLocator.ServiceProvider.GetService<TimeUtil>();
            var currentTime = timeUtil.GetTimeStamp();
            var fullProfile = profileHelper.GetFullProfile(sessionID);
            var pmcData = fullProfile.CharacterData.PmcData;
            var orderConfig = mcsConfigService.GetOrderConfig().OrderQuests.First();
            var repeatableQuestControllerTraverse = Traverse.Create(__instance);
            var generatedOrder = repeatableQuestControllerTraverse.Method("GetRepeatableQuestSubTypeFromProfile", [orderConfig, pmcData]).GetValue<PmcDataRepeatableQuest>();

            // 实现完成后将新ProcessExpiredQuests重新放入此处

            if (OrderQuestsQueueDict.TryGetValue(sessionID, out var orderQuestsQueue))
            {
                generatedOrder.EndTime = currentTime + orderConfig.ResetTime;
                while (orderQuestsQueue.Count > 0)
                {
                    var quest = orderQuestsQueue.Dequeue();
                    Console.WriteLine("加入新任务");
                    quest.Side = Enum.GetName(orderConfig.Side);
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
                OrderQuestsQueueDict.Remove(sessionID);
            }

            if (currentTime < generatedOrder.EndTime - 1)
            {
                Console.WriteLine("旧任务数据仍合法");
                __result.Add(generatedOrder);
                return;
            }

            generatedOrder.EndTime = currentTime + orderConfig.ResetTime;
            generatedOrder.InactiveQuests = [];
            generatedOrder.ChangeRequirement = [];

            Console.WriteLine("将尝试清除过期订单任务");
            repeatableQuestControllerTraverse.Method("ProcessExpiredQuests", [generatedOrder, pmcData]).GetValue();
            // 该函数的作用是将此配置分类中只要没完成的任务全部清除，应该自定义一个新的处理过期任务函数，而不是单纯使用配置分类下的endTime作为过期的标准
            // 或者最佳方式是继承RepeatableQuest得到OrderQuest，并多出一个对应自身任务的endTime，清理过期任务时则是检查该项来决定是否保留

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