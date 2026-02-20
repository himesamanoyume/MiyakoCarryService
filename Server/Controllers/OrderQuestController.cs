
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class OrderQuestController(
        OrderQuestService orderQuestService
    )
    {
        public void CreateOrderQuest(MongoId mcsLeadPlayerId, int players, SpawnType spawnType, int carryServiceLevel, int duration)
        {
            orderQuestService.CreateOrderQuest(mcsLeadPlayerId, players, spawnType, carryServiceLevel, duration);
        }

        public void ProcessExpiredQuests(PmcDataRepeatableQuest generatedRepeatables, PmcData bossPmcData)
        {
            orderQuestService.ProcessExpiredQuests(generatedRepeatables, bossPmcData);
        }

        public PmcDataRepeatableQuest GetRepeatableQuestSubTypeFromProfile(RepeatableQuestConfig repeatableConfig, PmcData pmcData)
        {
            return orderQuestService.GetRepeatableQuestSubTypeFromProfile(repeatableConfig, pmcData);
        }
    }
}