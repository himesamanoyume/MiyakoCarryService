
using System.Collections.Generic;
using System.Linq;
using MiyakoCarryService.Server.Generators.OrderQuestGeneration;
using MiyakoCarryService.Server.Patches;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSOrderQuestController(
        ISptLogger<MCSOrderQuestController> logger,
        MCSOrderQuestGenerator mcsOrderQuestGenerator,
        ProfileFixerService profileFixerService,
        TimeUtil timeUtil,
        ProfileHelper profileHelper
        )
    {
        public void CreateOrderQuest(MongoId sessionID, int players, int carryServiceLevel, int hours)
        {
            var fullProfile = profileHelper.GetFullProfile(sessionID);
            var pmcData = fullProfile.CharacterData.PmcData;
            logger.Info("开始创建任务");
            var orderQuest = mcsOrderQuestGenerator.GenerateOrderQuest(sessionID, pmcData, players, carryServiceLevel, hours);
            logger.Info("任务插入等待创建队列");
            if (GetClientRepeatableQuestsPatch.OrderQuestsQueueDict.TryGetValue(sessionID, out var orderQuestsQueue))
            {
                orderQuestsQueue.Enqueue(orderQuest);
            }
            else
            {
                GetClientRepeatableQuestsPatch.OrderQuestsQueueDict.Add(sessionID, new([orderQuest]));
            }
        }

        public void ProcessExpiredQuests(PmcDataRepeatableQuest generatedRepeatables, PmcData pmcData)
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

                var questStatusInProfile = pmcData.Quests.FirstOrDefault(quest => quest.QId == activeQuest.Id);
                if (questStatusInProfile is null)
                {
                    continue;
                }

                if (questStatusInProfile.Status == QuestStatusEnum.AvailableForFinish)
                {
                    questsToKeep.Add(activeQuest);
                    continue;
                }

                profileFixerService.RemoveDanglingConditionCounters(pmcData);

                pmcData.Quests = pmcData.Quests.Where(quest => quest.QId != activeQuest.Id).ToList();

                generatedRepeatables.InactiveQuests.Add(activeQuest);
            }

            generatedRepeatables.ActiveQuests = questsToKeep;
        }
    }
}