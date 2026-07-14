using MiyakoCarryService.Server.Callbacks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Routers.Static
{
    [Injectable]
    public class InfoStaticRouter(
        JsonUtil jsonUtil,
        InfoCallbacks infoCallbacks
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<McsBotPlayerAidRequestData>(
                "/mcs/client/order/settle",
                async (url, info, sessionId, output) => await infoCallbacks.SettleOrder(url, info, sessionId)
            ),
            new RouteAction<McsBotPlayerAidRequestData>(
                "/mcs/client/order/renew",
                async (url, info, sessionId, output) => await infoCallbacks.RenewOrder(url, info, sessionId)
            )
        ]
    )
    { }
}