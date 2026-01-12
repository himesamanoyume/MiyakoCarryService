
using MiyakoCarryService.Server.Callbacks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Routers.Static
{
    [Injectable]
    public sealed class MatchStaticRouter(
        JsonUtil jsonUtil,
        MatchCallbacks matchCallbacks
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/client/match/raid/ready",
                async (url, info, sessionID, output) => await matchCallbacks.RaidReady(url, info, sessionID)
            ),
            new RouteAction<EmptyRequestData>(
                "/client/match/raid/not-ready",
                async (url, info, sessionID, output) => await matchCallbacks.NotRaidReady(url, info, sessionID)
            )
        ]
    )
    { }
}