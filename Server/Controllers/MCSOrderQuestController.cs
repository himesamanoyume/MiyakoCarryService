
using MiyakoCarryService.Server.Generators.OrderQuestGeneration;
using MiyakoCarryService.Server.Patches;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSOrderQuestController(
        ISptLogger<MCSOrderQuestController> logger,
        MCSOrderQuestGenerator mcsOrderQuestGenerator,
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
    }
}