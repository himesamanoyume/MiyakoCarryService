
using MiyakoCarryService.Server.Callbacks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Routers.Static
{
    [Injectable]
    public sealed class ConfigStaticRouter(
        JsonUtil jsonUtil,
        ConfigCallbacks configCallbacks
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/mcs/config",
                async (url, info, sessionId, output) => await configCallbacks.GetMcsPluginConfig(url, info, sessionId)
            ),
            new RouteAction<EmptyRequestData>(
                "/mcs/client/brain/config",
                async (url, info, sessionId, output) => await configCallbacks.GetAllCustomBrainName(url, info, sessionId)
            )
        ]
    )
    { }
}