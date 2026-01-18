
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
        public void ProcessExpiredCarryServiceProfile(MongoId mcsBossPlayerId, MongoId mcsPlayerId)
        {
            profileService.ProcessExpiredCarryServiceProfile(mcsBossPlayerId, mcsPlayerId);
        }

        public BotBase GeneratePmcBotBaseProfile(MongoId mcsBossPlayerId, PmcData mcsPlayerPmcData, int carryServiceLevel)
        {
            return profileService.GeneratePmcBotProfile(mcsBossPlayerId, mcsPlayerPmcData, carryServiceLevel);
        }

        public void SaveCSPlayerProfile(MongoId mcsBossPlayerId, SptProfile mcsPlayerProfile)
        {
            profileService.SaveCSPlayerProfile(mcsBossPlayerId, mcsPlayerProfile);
        }

        public SptProfile Generate(MongoId mcsBossPlayerId, MongoId mcsPlayerId, PmcData completeQuestPmcData, int carryServiceLevel)
        {
            return profileService.Generate(mcsBossPlayerId, mcsPlayerId, completeQuestPmcData, carryServiceLevel);
        }
        public SptProfile? GetCSFullProfile(MongoId mcsBossPlayerId, MongoId mcsPlayerId)
        {
            return profileService.GetCSFullProfile(mcsBossPlayerId, mcsPlayerId);
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