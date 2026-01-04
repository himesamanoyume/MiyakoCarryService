using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class MCSOrderQuestService(
        ModHelper modHelper,
        MCSConfigService mcsConfigService,
        ISptLogger<MCSOrderQuestService> logger,
        MCSOrderQuestGenerator mcsOrderQuestGenerator,
        ProfileFixerService profileFixerService,
        TimeUtil timeUtil,
        MCSOrderInfoService mcsOrderInfoService,
        ProfileHelper profileHelper
    )
    {
        private readonly string _traderFolderDir = System.IO.Path.Join(mcsConfigService.GetModPath(), "Assets", "database", "templates");
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

        public void CreateOrderQuest(MongoId sessionId, int players, int carryServiceLevel, int duration)
        {
            var fullProfile = profileHelper.GetFullProfile(sessionId);
            var pmcData = fullProfile.CharacterData.PmcData;
            logger.Info("开始创建任务");
            var orderQuest = mcsOrderQuestGenerator.GenerateOrderQuest(sessionId, pmcData, players, carryServiceLevel, duration);
            logger.Info("任务插入等待创建队列");
            if (GetClientRepeatableQuestsPatch.OrderQuestsQueueDict.TryGetValue(sessionId, out var orderQuestsQueue))
            {
                orderQuestsQueue.Enqueue(orderQuest);
            }
            else
            {
                GetClientRepeatableQuestsPatch.OrderQuestsQueueDict.Add(sessionId, new([orderQuest]));
            }
            mcsOrderInfoService.CreateOrderInfo(sessionId, players, carryServiceLevel, duration, orderQuest.Id);
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
    }

}