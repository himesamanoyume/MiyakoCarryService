
using MiyakoCarryService.Server.Generators.OrderQuestGeneration;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services.Mod;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSOrderQuestController(
        CustomQuestService customQuestService,
        MCSOrderQuestGenerator mcsOrderQuestGenerator,
        ProfileHelper profileHelper
        )
    {
        public void CreateOrderQuest(MongoId sessionID, int players, int carryServiceLevel, int hours)
        {
            var fullProfile = profileHelper.GetFullProfile(sessionID);
            var pmcData = fullProfile.CharacterData.PmcData;
            var orderQuest = mcsOrderQuestGenerator.GenerateOrderQuest(sessionID, pmcData, players, carryServiceLevel, hours);
            var newQuestDetails = new NewQuestDetails
            {
                NewQuest = orderQuest,
                Locales = null
            };
            customQuestService.CreateQuest(newQuestDetails);
        }
    }
}