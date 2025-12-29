
using MiyakoCarryService.Server.Callbacks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Routers.Static
{
    [Injectable]
    public class OrderStaticRouter(JsonUtil jsonUtil, OrderCallbacks orderCallbacks)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/client/orderQuests/activityPeriods",
                async (url, info, sessionID, output) => await orderCallbacks.ActivityPeriods(url, info, sessionID)
            ),
        ]
    ) { }
}