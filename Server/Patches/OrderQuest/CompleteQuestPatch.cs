
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

        private static InfoController InfoController { get => field ??= ServiceLocator.ServiceProvider.GetService<InfoController>(); }
        private static TraderController TraderController { get => field ??= ServiceLocator.ServiceProvider.GetService<TraderController>(); }
        private static ProfileController ProfileController { get => field ??= ServiceLocator.ServiceProvider.GetService<ProfileController>(); }

        [PatchPostfix]
        public static void Postfix(PmcData pmcData, CompleteQuestRequestData request, MongoId sessionID)
        {
            var completedQuestId = request.QuestId;
            var orderInfos = InfoController.GetOrderInfos(sessionID);
            foreach (var orderInfo in orderInfos)
            {
                if (completedQuestId == orderInfo.QuestId)
                {
                    InfoController.SetBaseInfoStarted(orderInfo);
                    foreach (var mcsBotPlayerId in orderInfo.PlayerIds)
                    {
                        var mcsBotPlayerProfile = ProfileController.Generate(orderInfo.McsLeadPlayerId, mcsBotPlayerId, pmcData, orderInfo);
                        InfoController.CompleteOrderQuestSendFriendRequest(mcsBotPlayerProfile, orderInfo.McsLeadPlayerId);
                    }
                    break;
                }
            }
            var ticketInfos = InfoController.GetTicketInfos(sessionID);
            foreach (var ticketInfo in ticketInfos)
            {
                if (completedQuestId == ticketInfo.QuestId)
                {
                    InfoController.SetBaseInfoStarted(ticketInfo);
                    TraderController.ModifyPunishmentMulti(ticketInfo.Percent / 100d, false);
                    break;
                }
            }
            _ = InfoController.SaveOrderAndTicketInfo();
        }
    }
}