
using MiyakoCarryService.Server.Callbacks;
using MiyakoCarryService.Server.Models.Eft.Trader;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Routers.Static
{
    [Injectable]
    public sealed class TraderStaticRouter(
        JsonUtil jsonUtil,
        TraderCallbacks traderCallbacks
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<FriendlyFirePenaltyRequestData>(
                "/mcs/client/trading/api/friendlyFirePenalty",
                async (url, info, sessionId, output) => await traderCallbacks.FriendlyFirePenalty(url, info, sessionId)
            ),
        ]
    )
    { }
}