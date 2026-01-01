

using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Servers;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSProfileController(
        MCSProfileService mcsProfileService,
        SaveServer saveServer
    )
    {
        public void ProcessExpiredCarryServiceProfile(MongoId sessionId, MongoId csPlayerSessionId)
        {
            var completeQuestProfile = saveServer.GetProfile(sessionId);
            completeQuestProfile?.FriendProfileIds?.Remove(csPlayerSessionId);
            mcsProfileService.RemoveProfile(sessionId, csPlayerSessionId);
        }

        public BotBase GenerateBotProfile(MongoId sessionId, PmcData pmcData, int carryServiceLevel)
        {
            return mcsProfileService.GenerateBotProfile(sessionId, pmcData, carryServiceLevel);
        }

        public void SaveMCPlayerProfile(MongoId sessionId, BotBase csProfile)
        {
            mcsProfileService.SaveMCPlayerProfile(sessionId, csProfile);
        }
    }
}