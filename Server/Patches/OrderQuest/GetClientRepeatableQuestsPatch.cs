using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Patches.OrderQuest
{
    /// <summary>
    /// 在获取行动任务时，检查是否有新Order需要加入，随后将已有的Order任务一并返回给客户端
    /// </summary>
    [Injectable]
    public sealed class GetClientRepeatableQuestsPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RepeatableQuestController), nameof(RepeatableQuestController.GetClientRepeatableQuests));

        public GetClientRepeatableQuestsPatch(ConfigController configController, Controllers.QuestController questController, Controllers.ProfileController profileController, InfoController infoController, ProfileHelper profileHelper, TimeUtil timeUtil)
        {
            _configController = configController;
            _questController = questController;
            _profileController = profileController;
            _infoController = infoController;
            _profileHelper = profileHelper;
            _timeUtil = timeUtil;
        }

        public static Dictionary<MongoId, Queue<RepeatableQuest>> QuestsQueueDict = new();

        private static ConfigController _configController;
        private static Controllers.QuestController _questController;
        private static Controllers.ProfileController _profileController;
        private static InfoController _infoController;
        private static ProfileHelper _profileHelper;
        private static TimeUtil _timeUtil;

        [PatchPostfix]
        public static void Postfix(MongoId sessionID, ref List<PmcDataRepeatableQuest> __result)
        {
            var currentTime = _timeUtil.GetTimeStamp();
            var fullProfile = _profileHelper.GetFullProfile(sessionID);
            var pmcData = fullProfile.CharacterData.PmcData;
            var orderConfig = _configController.GetOrderConfig().OrderQuests.FirstOrDefault();
            var orderPendingPaymentTime = _configController.GetMcsPluginConfig().ServerConfig.OrderPendingPaymentTime;
            var generatedOrder = _questController.GetRepeatableQuestSubTypeFromProfile(orderConfig, pmcData);

            if (QuestsQueueDict.TryGetValue(sessionID, out var orderQuestsQueue))
            {
                generatedOrder.EndTime = currentTime + orderPendingPaymentTime;
                while (orderQuestsQueue.Count > 0)
                {
                    var quest = orderQuestsQueue.Dequeue();
                    quest.Side = Enum.GetName(orderConfig.Side);
                    quest.ChangeCost.FirstOrDefault(x => x.TemplateId == ItemTpl.MONEY_ROUBLES).Count = (int)(currentTime + orderPendingPaymentTime);
                    generatedOrder.ActiveQuests.Add(quest);
                    generatedOrder.ChangeRequirement.Add(
                        quest.Id,
                        new ChangeRequirement
                        {
                            ChangeCost = quest.ChangeCost,
                            ChangeStandingCost = (double)quest.ChangeStandingCost
                        }
                    );
                }
                QuestsQueueDict.Remove(sessionID);
            }

            _questController.ProcessExpiredQuests(generatedOrder, pmcData);

            _infoController.MarkExpiredOrderInfos(_profileController.ProcessExpiredMcsBotPlayerNotify);

            var expiredTicketLeads = _infoController.GetExpiredTicketMcsLeadPlayerIds();
            foreach (var kvp in expiredTicketLeads)
            {
                if (_profileController.IsMcsBotPlayerInventoryMode(kvp.Key))
                {
                    continue;
                }
                _infoController.ProcessExpiredTicketInfo(kvp.Key);
            }

            if (currentTime < generatedOrder.EndTime - 1)
            {
                __result.Add(generatedOrder);
                return;
            }

            generatedOrder.EndTime = currentTime + orderPendingPaymentTime;
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