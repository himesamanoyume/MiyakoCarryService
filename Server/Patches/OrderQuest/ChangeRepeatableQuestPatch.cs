
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Quests;
using SPTarkov.Server.Core.Models.Spt.Quests;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Patches.OrderQuest
{
    /// <summary>
    /// 如果是Order类型更换任务请求，直接将此任务删除并删除订单
    /// </summary>
    public sealed class ChangeRepeatableQuestPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RepeatableQuestController), nameof(RepeatableQuestController.ChangeRepeatableQuest));

        [PatchPrefix]
        public static bool Prefix(RepeatableQuestController __instance, PmcData pmcData, RepeatableQuestChangeRequest changeRequest, MongoId sessionID, ref ItemEventRouterResponse __result)
        {
            var eventOutputHolder = ServiceLocator.ServiceProvider.GetService<EventOutputHolder>();
            var serverLocalisationService = ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>();
            var httpResponseUtil = ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>();
            var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<RepeatableQuestChangeRequest>>();
            
            var output = eventOutputHolder.GetOutput(sessionID);

            var repeatableQuestControllerTraverse = Traverse.Create(__instance);
            var repeatables = repeatableQuestControllerTraverse.Method("GetRepeatableById", [changeRequest.QuestId, pmcData]).GetValue<GetRepeatableByIdResult?>();
            var questToReplace = repeatables.Quest;
            if (repeatables.RepeatableType is null || repeatables.Quest is null)
            {
                var message = serverLocalisationService.GetText("quest-unable_to_find_repeatable_to_replace");
                logger.Error(message);

                __result = httpResponseUtil.AppendErrorToOutput(output, message);
                return false;
            }
            
            if (repeatables.RepeatableType.Name == "Order")
            {
                var infoController = ServiceLocator.ServiceProvider.GetService<InfoController>();
                var orderQuestController = ServiceLocator.ServiceProvider.GetService<Controllers.QuestController>();
                var orderInfos = infoController.GetAllOrderInfo();
                foreach (var orderInfo in orderInfos)
                {
                    if (orderInfo.QuestId == questToReplace.Id)
                    {
                        orderQuestController.Refund(sessionID, questToReplace, pmcData);
                        infoController.RemoveOrderInfo(orderInfo);
                        break;
                    }
                }
                var ticketInfos = infoController.GetAllTicketInfo();
                foreach (var ticketInfo in ticketInfos)
                {
                    if (ticketInfo.QuestId == questToReplace.Id)
                    {
                        orderQuestController.Refund(sessionID, questToReplace, pmcData);
                        infoController.RemoveTicketInfo(ticketInfo);
                        break;
                    }
                }
                _ = infoController.SaveOrderAndTicketInfo();
                __result = output;
                return false;
            }

            return true;
        }
    }
}