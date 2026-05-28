
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

namespace MiyakoCarryService.Server.Patches.OrderQuest
{
    /// <summary>
    /// 对应的Order或Ticket任务完成时，根据订单信息生成对应的护航存档，或是减免全局涨价惩罚
    /// </summary>
    public sealed class CompleteQuestPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(QuestHelper), nameof(QuestHelper.CompleteQuest));

        [PatchPostfix]
        public static void Postfix(PmcData pmcData, CompleteQuestRequestData request, MongoId sessionID)
        {
            var infoController = ServiceLocator.ServiceProvider.GetService<InfoController>();
            var traderController = ServiceLocator.ServiceProvider.GetService<TraderController>();
            var profileController = ServiceLocator.ServiceProvider.GetService<ProfileController>();
            var completedQuestId = request.QuestId;
            var orderInfos = infoController.GetOrderInfos(sessionID);
            foreach (var orderInfo in orderInfos)
            {
                if (completedQuestId == orderInfo.QuestId)
                {
                    infoController.SetBaseInfoStarted(orderInfo);
                    foreach (var mcsBotPlayerId in orderInfo.PlayerIds)
                    {
                        var mcsBotPlayerProfile = profileController.Generate(orderInfo.McsLeadPlayerId, mcsBotPlayerId, pmcData, orderInfo);
                        infoController.CompleteOrderQuestSendFriendRequest(mcsBotPlayerProfile, orderInfo.McsLeadPlayerId);
                    }
                    break;
                }
            }
            var ticketInfos = infoController.GetTicketInfos(sessionID);
            foreach (var ticketInfo in ticketInfos)
            {
                if (completedQuestId == ticketInfo.QuestId)
                {
                    infoController.SetBaseInfoStarted(ticketInfo);
                    traderController.ModifyPunishmentMulti(ticketInfo.Percent / 100d, false);
                    break;
                }
            }
            _ = infoController.SaveOrderAndTicketInfo();
        }
    }
}