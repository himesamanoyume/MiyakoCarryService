
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
        public void ProcessExpiredCarryServiceProfile(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId)
        {
            profileService.ProcessExpiredCarryServiceProfile(mcsBossPlayerId, mcsBotPlayerId);
        }

        public BotBase GeneratePmcBotBaseProfile(MongoId mcsBossPlayerId, PmcData mcsBotPlayerPmcData, int carryServiceLevel)
        {
            return profileService.GeneratePmcBotProfile(mcsBossPlayerId, mcsBotPlayerPmcData, carryServiceLevel);
        }

        public void SaveCSPlayerProfile(MongoId mcsBossPlayerId, SptProfile mcsBotPlayerProfile)
        {
            profileService.SaveCSPlayerProfile(mcsBossPlayerId, mcsBotPlayerProfile);
        }

        public SptProfile Generate(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId, PmcData completeQuestPmcData, int carryServiceLevel)
        {
            return profileService.Generate(mcsBossPlayerId, mcsBotPlayerId, completeQuestPmcData, carryServiceLevel);
        }
        public SptProfile? GetCSFullProfile(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId)
        {
            return profileService.GetCSFullProfile(mcsBossPlayerId, mcsBotPlayerId);
        }

        public SptProfile? GetCSFullProfileByAccountId(MongoId mcsBossPlayerId, string mcsAid)
        {
            return profileService.GetCSFullProfileByAccountId(mcsBossPlayerId, mcsAid);
        }

        public SptProfile? GetCSFullProfileByAccountId(MongoId mcsBossPlayerId, int mcsAid)
        {
            return profileService.GetCSFullProfileByAccountId(mcsBossPlayerId, mcsAid);
        }

        public List<SptProfile>? GetCSFullProfileByBossId(MongoId mcsBossPlayerId)
        {
            return profileService.GetCSFullProfileByBossId(mcsBossPlayerId);
        }
    }
}