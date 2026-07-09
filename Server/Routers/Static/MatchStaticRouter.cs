
using MiyakoCarryService.Server.Callbacks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Routers.Static
{
    [Injectable]
    public class MatchStaticRouter(
        JsonUtil jsonUtil,
        MatchCallbacks matchCallbacks
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/client/match/raid/ready",
                async (url, info, sessionId, output, cancellationToken) => await matchCallbacks.RaidReady(url, info, sessionId)
            ),
            new RouteAction<EmptyRequestData>(
                "/client/match/raid/not-ready",
                async (url, info, sessionId, output, cancellationToken) => await matchCallbacks.NotRaidReady(url, info, sessionId)
            ),
            new RouteAction<EmptyRequestData>(
                "/mcs/client/match/raid/abort",
                async (url, info, sessionId, output, cancellationToken) => await matchCallbacks.MatchingAbort(url, info, sessionId)
            ),
            new RouteAction<EmptyRequestData>(
                "/mcs/client/match/group/delete",
                async (url, info, sessionId, output, cancellationToken) => await matchCallbacks.DeleteGroup(url, info, sessionId)
            )
        ]
    )
    { }
}