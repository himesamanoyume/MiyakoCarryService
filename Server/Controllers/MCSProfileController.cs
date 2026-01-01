

using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
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
            var profile = saveServer.GetProfile(sessionId);
            profile?.FriendProfileIds?.Remove(csPlayerSessionId);
            mcsProfileService.RemoveProfile(sessionId, csPlayerSessionId);
        }
    }
}