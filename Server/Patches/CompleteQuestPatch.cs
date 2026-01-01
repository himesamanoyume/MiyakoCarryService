
using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Quests;

namespace MiyakoCarryService.Server.Patches
{
    public sealed class CompleteQuestPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(QuestHelper), nameof(QuestHelper.CompleteQuest));

        [PatchPostfix]
        public static void Postfix(PmcData pmcData, CompleteQuestRequestData request, MongoId sessionID)
        {
            var mcsOrderInfoController = ServiceLocator.ServiceProvider.GetService<MCSOrderInfoController>();
            var completedQuestId = request.QuestId;
            var orderInfos = mcsOrderInfoController.GetOrderInfos(sessionID);
            foreach (var orderInfo in orderInfos)
            {
                if (completedQuestId == orderInfo.QuestId)
                {
                    mcsOrderInfoController.SetOrderInfoStarted(orderInfo, pmcData);
                }
            }
        }
    }
}