
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Quest;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Quests;

namespace MiyakoCarryService.Server.Patches.OrderQuest
{
    /// <summary>
    /// 对应的Order或Ticket任务完成时，根据订单信息生成对应的护航存档，或是减免全局涨价惩罚
    /// </summary>
    [Injectable]
    public sealed class CompleteQuestPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(QuestHelper), nameof(QuestHelper.CompleteQuest));

        public CompleteQuestPatch(InfoController infoController, TraderController traderController, ProfileController profileController)
        {
            _infoController = infoController;
            _traderController = traderController;
            _profileController = profileController;
        }

        private static InfoController _infoController;
        private static TraderController _traderController;
        private static ProfileController _profileController;

        [PatchPrefix]
        public static void Prefix(PmcData pmcData, CompleteQuestRequestData request, MongoId sessionId)
        {
            if (pmcData?.TradersInfo is null)
            {
                return;
            }

            if (pmcData.TradersInfo.ContainsKey(Services.TraderService.MiyakoTraderId))
            {
                return;
            }

            pmcData.TradersInfo[Services.TraderService.MiyakoTraderId] = new TraderInfo
            {
                LoyaltyLevel = 1,
                SalesSum = 0.0,
                Standing = 0.0,
                NextResupply = 0,
                Unlocked = true,
                Disabled = false,
            };
        }

        [PatchPostfix]
        public static void Postfix(PmcData pmcData, CompleteQuestRequestData request, MongoId sessionId)
        {
            var completedQuestId = request.QuestId;
            var orderInfos = _infoController.GetOrderInfos(sessionId);
            foreach (var orderInfo in orderInfos)
            {
                if (completedQuestId == orderInfo.QuestId)
                {
                    if (orderInfo.RenewTargetQuestId is not null)
                    {
                        var targetQuestId = orderInfo.RenewTargetQuestId.Value;
                        var originalOrder = orderInfos.FirstOrDefault(o => o.QuestId == targetQuestId);
                        if (originalOrder is not null)
                        {
                            _infoController.ApplyRenew(originalOrder.QuestId, originalOrder.Duration);
                            foreach (var mcsBotPlayerId in originalOrder.PlayerIds)
                            {
                                var mcsBotPlayerProfile = _profileController.GetMcsBotPlayerProfile(originalOrder.McsLeadPlayerId, mcsBotPlayerId);
                                if (mcsBotPlayerProfile is null)
                                {
                                    continue;
                                }
                                _infoController.CompleteOrderQuestSendFriendRequest(mcsBotPlayerProfile, originalOrder.McsLeadPlayerId);
                            }
                        }

                        _infoController.RemoveOrderInfo(orderInfo);
                    }
                    else
                    {
                        _infoController.SetBaseInfoStarted(orderInfo);
                        foreach (var mcsBotPlayerId in orderInfo.PlayerIds)
                        {
                            var mcsBotPlayerProfile = _profileController.Generate(orderInfo.McsLeadPlayerId, mcsBotPlayerId, pmcData, orderInfo);
                            _infoController.CompleteOrderQuestSendFriendRequest(mcsBotPlayerProfile, orderInfo.McsLeadPlayerId);
                        }
                    }
                    break;
                }
            }
            var ticketInfos = _infoController.GetTicketInfos(sessionId);
            foreach (var ticketInfo in ticketInfos)
            {
                if (completedQuestId == ticketInfo.QuestId)
                {
                    _infoController.SetBaseInfoStarted(ticketInfo);
                    _traderController.ModifyPunishmentMulti(ticketInfo.Percent / 100d, false);
                    break;
                }
            }
            _ = _infoController.SaveOrderAndTicketInfo();
        }
    }
}