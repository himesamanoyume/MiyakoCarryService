
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
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
    [Injectable]
    public sealed class ChangeRepeatableQuestPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RepeatableQuestController), nameof(RepeatableQuestController.ChangeRepeatableQuest));

        public ChangeRepeatableQuestPatch(EventOutputHolder eventOutputHolder, ServerLocalisationService serverLocalisationService, HttpResponseUtil httpResponseUtil, ISptLogger<RepeatableQuestChangeRequest> logger, InfoController infoController, Controllers.QuestController questController)
        {
            _eventOutputHolder = eventOutputHolder;
            _serverLocalisationService = serverLocalisationService;
            _httpResponseUtil = httpResponseUtil;
            _logger = logger;
            _infoController = infoController;
            _questController = questController;
        }

        private static EventOutputHolder _eventOutputHolder;
        private static ServerLocalisationService _serverLocalisationService;
        private static HttpResponseUtil _httpResponseUtil;
        private static ISptLogger<RepeatableQuestChangeRequest> _logger;
        private static InfoController _infoController;
        private static Controllers.QuestController _questController;

        [PatchPrefix]
        public static bool Prefix(RepeatableQuestController __instance, PmcData pmcData, RepeatableQuestChangeRequest changeRequest, MongoId sessionID, ref ItemEventRouterResponse __result)
        {
            var output = _eventOutputHolder.GetOutput(sessionID);

            var repeatableQuestControllerTraverse = Traverse.Create(__instance);
            var repeatables = repeatableQuestControllerTraverse.Method("GetRepeatableById", [changeRequest.QuestId, pmcData]).GetValue<GetRepeatableByIdResult?>();
            var questToReplace = repeatables.Quest;
            if (repeatables.RepeatableType is null || repeatables.Quest is null)
            {
                var message = _serverLocalisationService.GetText("quest-unable_to_find_repeatable_to_replace");
                _logger.Error(message);

                __result = _httpResponseUtil.AppendErrorToOutput(output, message);
                return false;
            }
            
            if (repeatables.RepeatableType.Name == "Order")
            {
                var orderInfos = _infoController.GetAllOrderInfo();
                foreach (var orderInfo in orderInfos)
                {
                    if (orderInfo.QuestId == questToReplace.Id)
                    {
                        _questController.Refund(sessionID, questToReplace, pmcData);
                        _infoController.RemoveOrderInfo(orderInfo);
                        break;
                    }
                }
                var ticketInfos = _infoController.GetAllTicketInfo();
                foreach (var ticketInfo in ticketInfos)
                {
                    if (ticketInfo.QuestId == questToReplace.Id)
                    {
                        _questController.Refund(sessionID, questToReplace, pmcData);
                        _infoController.RemoveTicketInfo(ticketInfo);
                        break;
                    }
                }
                _ = _infoController.SaveOrderAndTicketInfo();
                __result = output;
                return false;
            }

            return true;
        }
    }
}