
using System.Collections.Generic;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class ProfileController(
        ProfileService profileService
    )
    {
        public void ProcessExpiredMcsBotPlayerProfile(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId)
        {
            profileService.ProcessExpiredMcsBotPlayerProfile(mcsBossPlayerId, mcsBotPlayerId);
        }

        public BotBase GeneratePmcBotBaseProfile(MongoId mcsBossPlayerId, PmcData mcsBotPlayerPmcData, int carryServiceLevel)
        {
            return profileService.GeneratePmcBotProfile(mcsBossPlayerId, mcsBotPlayerPmcData, carryServiceLevel);
        }

        public void SaveMcsBotPlayerProfile(MongoId mcsBossPlayerId, SptProfile mcsBotPlayerProfile)
        {
            profileService.SaveMcsBotPlayerProfile(mcsBossPlayerId, mcsBotPlayerProfile);
        }

        public SptProfile Generate(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId, PmcData completeQuestPmcData, int carryServiceLevel)
        {
            return profileService.Generate(mcsBossPlayerId, mcsBotPlayerId, completeQuestPmcData, carryServiceLevel);
        }

        public SptProfile? GetMcsBotPlayerProfile(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId)
        {
            return profileService.GetMcsBotPlayerProfile(mcsBossPlayerId, mcsBotPlayerId);
        }

        public SptProfile? GetMcsBotPlayerProfileByAccountId(MongoId mcsBossPlayerId, string mcsAid)
        {
            return profileService.GetMcsBotPlayerProfileByAccountId(mcsBossPlayerId, mcsAid);
        }

        public List<SptProfile> GetAllMcsBotPlayerProfileByBossId(MongoId mcsBossPlayerId)
        {
            return profileService.GetAllMcsBotPlayerProfileByBossId(mcsBossPlayerId);
        }
    }
}