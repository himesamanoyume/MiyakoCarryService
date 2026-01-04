
using MiyakoCarryService.Server.Callbacks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Routers.Static
{
    [Injectable]
    public sealed class MCSMatchStaticRouter(
        JsonUtil jsonUtil,
        MCSMatchCallbacks mcsMatchCallbacks
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/client/match/raid/ready",
                async (url, info, sessionID, output) => await mcsMatchCallbacks.RaidReady(url, info, sessionID)
            ),
            new RouteAction<EmptyRequestData>(
                "/client/match/raid/not-ready",
                async (url, info, sessionID, output) => await mcsMatchCallbacks.NotRaidReady(url, info, sessionID)
            )
        ]
    )
    { }
}