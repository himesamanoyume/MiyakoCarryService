
using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Quests;
using SPTarkov.Server.Core.Models.Spt.Quests;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Patches.OrderQuest
{
    /// <summary>
    /// 如果是Order类型更换任务请求，直接将此任务删除并删除订单
    /// </summary>
    public sealed class ChangeRepeatableQuestPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RepeatableQuestController), nameof(RepeatableQuestController.ChangeRepeatableQuest));

        public ChangeRepeatableQuestPatch(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        private static IServiceProvider ServiceProvider;

        private static EventOutputHolder EventOutputHolder { get => field ??= ServiceProvider.GetService<EventOutputHolder>(); }
        private static ServerLocalisationService ServerLocalisationService { get => field ??= ServiceProvider.GetService<ServerLocalisationService>(); }
        private static HttpResponseUtil HttpResponseUtil { get => field ??= ServiceProvider.GetService<HttpResponseUtil>(); }
        private static ISptLogger<RepeatableQuestChangeRequest> Logger { get => field ??= ServiceProvider.GetService<ISptLogger<RepeatableQuestChangeRequest>>(); }
        private static InfoController InfoController { get => field ??= ServiceProvider.GetService<InfoController>(); }
        private static Controllers.QuestController QuestController { get => field ??= ServiceProvider.GetService<Controllers.QuestController>(); }

        [PatchPrefix]
        public static bool Prefix(RepeatableQuestController __instance, PmcData pmcData, RepeatableQuestChangeRequest changeRequest, MongoId sessionID, ref ItemEventRouterResponse __result)
        {
            var output = EventOutputHolder.GetOutput(sessionID);

            var repeatableQuestControllerTraverse = Traverse.Create(__instance);
            var repeatables = repeatableQuestControllerTraverse.Method("GetRepeatableById", [changeRequest.QuestId, pmcData]).GetValue<GetRepeatableByIdResult?>();
            var questToReplace = repeatables.Quest;
            if (repeatables.RepeatableType is null || repeatables.Quest is null)
            {
                var message = ServerLocalisationService.GetText("quest-unable_to_find_repeatable_to_replace");
                Logger.Error(message);

                __result = HttpResponseUtil.AppendErrorToOutput(output, message);
                return false;
            }
            
            if (repeatables.RepeatableType.Name == "Order")
            {
                var orderInfos = InfoController.GetAllOrderInfo();
                foreach (var orderInfo in orderInfos)
                {
                    if (orderInfo.QuestId == questToReplace.Id)
                    {
                        QuestController.Refund(sessionID, questToReplace, pmcData);
                        InfoController.RemoveOrderInfo(orderInfo);
                        break;
                    }
                }
                var ticketInfos = InfoController.GetAllTicketInfo();
                foreach (var ticketInfo in ticketInfos)
                {
                    if (ticketInfo.QuestId == questToReplace.Id)
                    {
                        QuestController.Refund(sessionID, questToReplace, pmcData);
                        InfoController.RemoveTicketInfo(ticketInfo);
                        break;
                    }
                }
                _ = InfoController.SaveOrderAndTicketInfo();
                __result = output;
                return false;
            }

            return true;
        }
    }
}