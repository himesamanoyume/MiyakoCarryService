
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSOrderQuestController(
        MCSOrderQuestService mcsOrderQuestService
    )
    {
        public void CreateOrderQuest(MongoId bossSessionId, int players, int carryServiceLevel, int duration)
        {
            mcsOrderQuestService.CreateOrderQuest(bossSessionId, players, carryServiceLevel, duration);
        }

        public void ProcessExpiredQuests(PmcDataRepeatableQuest generatedRepeatables, PmcData bossPmcData)
        {
            mcsOrderQuestService.ProcessExpiredQuests(generatedRepeatables, bossPmcData);
        }
    }
}