
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSProfileController(
        MCSProfileService mcsProfileService
    )
    {
        public void ProcessExpiredCarryServiceProfile(MongoId bossSessionId, MongoId csPlayerSessionId)
        {
            mcsProfileService.ProcessExpiredCarryServiceProfile(bossSessionId, csPlayerSessionId);
        }

        public void RemoveCarryServiceProfile(MongoId bossSessionId, MongoId csPlayerSessionId)
        {
            mcsProfileService.RemoveCarryServiceProfile(bossSessionId, csPlayerSessionId);
        }

        public BotBase GeneratePmcBotBaseProfile(MongoId sessionId, PmcData pmcData, int carryServiceLevel)
        {
            return mcsProfileService.GeneratePmcBotProfile(sessionId, pmcData, carryServiceLevel);
        }

        public void SaveCSPlayerProfile(MongoId sessionId, SptProfile csProfile)
        {
            mcsProfileService.SaveCSPlayerProfile(sessionId, csProfile);
        }

        public SptProfile Generate(MongoId bossSessionId, MongoId csPlayerSessionId, PmcData completeQuestPmcData, int carryServiceLevel)
        {
            return mcsProfileService.Generate(bossSessionId, csPlayerSessionId, completeQuestPmcData, carryServiceLevel);
        }
        public SptProfile? GetCSFullProfile(MongoId bossSessionId, MongoId csPlayerSessionId)
        {
            return mcsProfileService.GetCSFullProfile(bossSessionId, csPlayerSessionId);
        }

        public SptProfile? GetCSFullProfileByAccountId(MongoId bossSessionId, string csAid)
        {
            return mcsProfileService.GetCSFullProfileByAccountId(bossSessionId, csAid);
        }

        public SptProfile? GetCSFullProfileByAccountId(MongoId bossSessionId, int csAid)
        {
            return mcsProfileService.GetCSFullProfileByAccountId(bossSessionId, csAid);
        }
    }
}