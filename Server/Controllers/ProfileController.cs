
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
        public void ProcessExpiredCarryServiceProfile(MongoId bossSessionId, MongoId csPlayerSessionId)
        {
            profileService.ProcessExpiredCarryServiceProfile(bossSessionId, csPlayerSessionId);
        }

        public BotBase GeneratePmcBotBaseProfile(MongoId sessionId, PmcData pmcData, int carryServiceLevel)
        {
            return profileService.GeneratePmcBotProfile(sessionId, pmcData, carryServiceLevel);
        }

        public void SaveCSPlayerProfile(MongoId sessionId, SptProfile csProfile)
        {
            profileService.SaveCSPlayerProfile(sessionId, csProfile);
        }

        public SptProfile Generate(MongoId bossSessionId, MongoId csPlayerSessionId, PmcData completeQuestPmcData, int carryServiceLevel)
        {
            return profileService.Generate(bossSessionId, csPlayerSessionId, completeQuestPmcData, carryServiceLevel);
        }
        public SptProfile? GetCSFullProfile(MongoId bossSessionId, MongoId csPlayerSessionId)
        {
            return profileService.GetCSFullProfile(bossSessionId, csPlayerSessionId);
        }

        public SptProfile? GetCSFullProfileByAccountId(MongoId bossSessionId, string csAid)
        {
            return profileService.GetCSFullProfileByAccountId(bossSessionId, csAid);
        }

        public SptProfile? GetCSFullProfileByAccountId(MongoId bossSessionId, int csAid)
        {
            return profileService.GetCSFullProfileByAccountId(bossSessionId, csAid);
        }

        public List<SptProfile>? GetCSFullProfileByBossId(MongoId bossSessionId)
        {
            return profileService.GetCSFullProfileByBossId(bossSessionId);
        }
    }
}