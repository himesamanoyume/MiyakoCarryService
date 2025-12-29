
using System;
using System.Collections.Generic;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;

namespace MiyakoCarryService.Server.Helper
{
    [Injectable]
    public class OrderQuestHelper(
        ISptLogger<OrderQuestHelper> logger,
        OrderQuestService orderQuestService,
        ConfigService configService,
        ServerLocalisationService serverLocalisationService,
        ICloner cloner)
    {
        protected readonly OrderConfig OrderConfig = configService.GetOrderConfig();

        public RepeatableQuest GenerateOrderTemplate(
            RepeatableQuestType type,
            MongoId traderId,
            MongoId sessionId)
        {
            var questData = GetClonedQuestTemplateForType(type, traderId);
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
                {"PickUp", "695207e8bcc1dd1e3c80dfcb"}
            };
            questData.TemplateId = typeIds.GetValueOrDefault(templateName);

            questData.Name = questData.Name.Replace("{traderId}", traderId).Replace("{templateId}", questData.TemplateId);

            questData.Note = questData.Note?.Replace("{traderId}", traderId).Replace("{templateId}", questData.TemplateId);

            questData.Description = questData.Description.Replace("{traderId}", traderId).Replace("{templateId}", questData.TemplateId);

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
            var orderTemplate = orderQuestService.GetOrderTemplate();
            var quest = type switch
            {
                RepeatableQuestType.Pickup => cloner.Clone(orderTemplate),
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
    }
}