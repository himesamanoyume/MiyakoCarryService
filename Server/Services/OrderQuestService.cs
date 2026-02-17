using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Generators.OrderQuestGeneration;
using MiyakoCarryService.Server.Models.Enums;
using MiyakoCarryService.Server.Patches.OrderQuest;
using MiyakoCarryService.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Logger;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class OrderQuestService(
        ModHelper modHelper,
        ConfigService configService,
        SptLogger<OrderQuestService> logger,
        OrderQuestGenerator orderQuestGenerator,
        ProfileFixerService profileFixerService,
        TimeUtil timeUtil,
        OrderInfoService orderInfoService,
        ICloner cloner,
        ServerLocalisationService serverLocalisationService,
        ProfileHelper profileHelper
    )
    {
        private readonly string _traderFolderDir = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "templates");
        private RepeatableQuest _orderTemplate;
        public async Task OnPostLoadAsync()
        {
            LoadOrderTemplate();
        }

        private void LoadOrderTemplate()
        {
            _orderTemplate = modHelper.GetJsonDataFromFile<RepeatableQuest>(_traderFolderDir, "orderQuests.json");
        }

        public RepeatableQuest GetOrderTemplate()
        {
            return _orderTemplate;
        }

        public void CreateOrderQuest(MongoId mcsLeadPlayerId, int players, EBotType botType, int carryServiceLevel, int duration)
        {
            var fullProfile = profileHelper.GetFullProfile(mcsLeadPlayerId);
            var pmcData = fullProfile.CharacterData.PmcData;
            logger.Info(serverLocalisationService.GetText(Locales.STARTINGQUESTCREATION));
            var orderQuest = orderQuestGenerator.GenerateOrderQuest(pmcData, players, carryServiceLevel, duration, configService.GetOrderConfig().OrderQuests.First().QuestConfig.CompletionConfig.First(), GenerateOrderTemplate(
                RepeatableQuestType.Completion, TraderService.MiyakoTraderId,
                mcsLeadPlayerId, players, botType, carryServiceLevel, duration
            ));
            if (GetClientRepeatableQuestsPatch.OrderQuestsQueueDict.TryGetValue(mcsLeadPlayerId, out var orderQuestsQueue))
            {
                orderQuestsQueue.Enqueue(orderQuest);
            }
            else
            {
                GetClientRepeatableQuestsPatch.OrderQuestsQueueDict.Add(mcsLeadPlayerId, new([orderQuest]));
            }
            orderInfoService.CreateOrderInfo(mcsLeadPlayerId, players, botType, carryServiceLevel, duration, orderQuest.Id);
        }

        public void ProcessExpiredQuests(PmcDataRepeatableQuest generatedRepeatables, PmcData bossPmcData)
        {
            var questsToKeep = new List<RepeatableQuest>();
            foreach (var activeQuest in generatedRepeatables.ActiveQuests)
            {
                var currentTime = timeUtil.GetTimeStamp();
                if (currentTime < activeQuest.ChangeCost.FirstOrDefault(x => x.TemplateId == "5449016a4bdc2d6f028b456f").Count - 1)
                {
                    questsToKeep.Add(activeQuest);
                    continue;
                }

                var questStatusInProfile = bossPmcData.Quests.FirstOrDefault(quest => quest.QId == activeQuest.Id);
                if (questStatusInProfile is null)
                {
                    continue;
                }

                if (questStatusInProfile.Status == QuestStatusEnum.AvailableForFinish)
                {
                    questsToKeep.Add(activeQuest);
                    continue;
                }

                profileFixerService.RemoveDanglingConditionCounters(bossPmcData);

                bossPmcData.Quests = bossPmcData.Quests.Where(quest => quest.QId != activeQuest.Id).ToList();

                generatedRepeatables.InactiveQuests.Add(activeQuest);
            }

            generatedRepeatables.ActiveQuests = questsToKeep;
        }

        public RepeatableQuest GenerateOrderTemplate(
            RepeatableQuestType type, MongoId traderId, MongoId sessionId,
            int players, EBotType botType, int carryServiceLevel, int duration
        )
        {
            var questData = GetClonedQuestTemplateForType(type, TraderService.TempOrderTraderId);
            if (questData is null)
            {
                logger.Error(serverLocalisationService.GetText("repeatable-quest_helper_template_not_found", type));
                return null;
            }

            var templateName = Enum.GetName(type);
            if (templateName is null)
            {
                logger.Error(serverLocalisationService.GetText("repeatable-quest_helper_template_name_not_found", type));
                return null;
            }

            // Get template id from config based on side and type of quest
            var typeIds = new Dictionary<string, MongoId>()
            {
                {"Completion", "695207e8bcc1dd1e3c80dfcb"}
            };
            questData.TemplateId = typeIds.GetValueOrDefault(templateName);

            questData.Name = questData.Name.Replace("{traderId}", traderId).Replace("{templateId}", questData.TemplateId);

            questData.Note = questData.Note?.Replace("{traderId}", traderId).Replace("{templateId}", questData.TemplateId);

            questData.Description = string.Format(serverLocalisationService.GetText(Locales.MIYAKOTRADERORDERDESCRIPTION), players, botType == EBotType.common ? serverLocalisationService.GetText(Locales.BOTTYPECOMMON) : Tools.GetBotTypeName(botType), carryServiceLevel, duration);

            questData.SuccessMessageText = questData
                .SuccessMessageText?.Replace("{traderId}", traderId)
                .Replace("{templateId}", questData.TemplateId);

            questData.FailMessageText = questData
                .FailMessageText?.Replace("{traderId}", traderId)
                .Replace("{templateId}", questData.TemplateId);

            questData.StartedMessageText = questData
                .StartedMessageText?.Replace("{traderId}", traderId)
                .Replace("{templateId}", questData.TemplateId);

            questData.ChangeQuestMessageText = questData
                .ChangeQuestMessageText?.Replace("{traderId}", traderId)
                .Replace("{templateId}", questData.TemplateId);

            questData.AcceptPlayerMessage = questData
                .AcceptPlayerMessage?.Replace("{traderId}", traderId)
                .Replace("{templateId}", questData.TemplateId);

            questData.DeclinePlayerMessage = questData
                .DeclinePlayerMessage?.Replace("{traderId}", traderId)
                .Replace("{templateId}", questData.TemplateId);

            questData.CompletePlayerMessage = questData
                .CompletePlayerMessage?.Replace("{traderId}", traderId)
                .Replace("{templateId}", questData.TemplateId);

            if (questData.QuestStatus is null)
            {
                return null;
            }

            questData.QuestStatus.Id = new MongoId();
            questData.QuestStatus.Uid = sessionId;
            questData.QuestStatus.QId = questData.Id;

            return questData;
        }

        public RepeatableQuest GetClonedQuestTemplateForType(RepeatableQuestType type, MongoId traderId)
        {
            var orderTemplate = GetOrderTemplate();
            var quest = type switch
            {
                RepeatableQuestType.Completion => cloner.Clone(orderTemplate),
                _ => null,
            };

            if (quest is null)
            {
                return null;
            }

            quest.Id = new MongoId();
            quest.TraderId = traderId;

            return quest;
        }

        public PmcDataRepeatableQuest GetRepeatableQuestSubTypeFromProfile(RepeatableQuestConfig repeatableConfig, PmcData pmcData)
        {
            var repeatableQuestDetails = pmcData.RepeatableQuests.FirstOrDefault(repeatable => repeatable.Name == repeatableConfig.Name);

            if (repeatableQuestDetails is null)
            {
                repeatableQuestDetails = new PmcDataRepeatableQuest
                {
                    Id = repeatableConfig.Id,
                    Name = repeatableConfig.Name,
                    ActiveQuests = [],
                    InactiveQuests = [],
                    EndTime = 0,
                    FreeChanges = 0,
                    FreeChangesAvailable = 0,
                    ChangeRequirement = new(),
                };

                pmcData.RepeatableQuests.Add(repeatableQuestDetails);
            }
            return repeatableQuestDetails;
        }
    }
}