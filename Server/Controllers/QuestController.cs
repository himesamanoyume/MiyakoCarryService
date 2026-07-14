
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
    public class QuestController(
        QuestService questService
    )
    {
        public void CreateOrderQuest(MongoId mcsLeadPlayerId, int players, SpawnType spawnType, int carryServiceLevel, int duration)
        {
            questService.CreateOrderQuest(mcsLeadPlayerId, players, spawnType, carryServiceLevel, duration);
        }

        public void CreateTicketQuest(MongoId mcsLeadPlayerId, int percent)
        {
            questService.CreateTicketQuest(mcsLeadPlayerId, percent);
        }

        public void ProcessExpiredQuests(PmcDataRepeatableQuest generatedRepeatables, PmcData bossPmcData)
        {
            questService.ProcessExpiredQuests(generatedRepeatables, bossPmcData);
        }

        public PmcDataRepeatableQuest GetRepeatableQuestSubTypeFromProfile(RepeatableQuestConfig repeatableConfig, PmcData pmcData)
        {
            return questService.GetRepeatableQuestSubTypeFromProfile(repeatableConfig, pmcData);
        }

        public void Refund(MongoId sessionId, RepeatableQuest questToReplace, PmcData pmcData)
        {
            questService.Refund(sessionId, questToReplace, pmcData);
        }

        public bool RenewOrder(MongoId mcsLeadPlayerId, string aid)
        {
            return questService.RenewOrder(mcsLeadPlayerId, aid);
        }
    }
}