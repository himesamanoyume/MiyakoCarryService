
using MiyakoCarryService.Server.Callbacks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Routers.Static
{
    [Injectable]
    public class LogStaticRouter(
        JsonUtil jsonUtil,
        LogCallbacks logCallbacks
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<DebugRequestData>(
                "/mcs/client/log",
                async (url, info, sessionId, output) => await logCallbacks.PrintLog(url, info, sessionId)
            )
        ]
    )
    { }
}