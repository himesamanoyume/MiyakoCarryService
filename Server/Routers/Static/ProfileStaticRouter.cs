
using MiyakoCarryService.Server.Callbacks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Routers.Static
{
    [Injectable]
    public class ProfileStaticRouter(
        JsonUtil jsonUtil,
        ProfileCallbacks profileCallbacks
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/mcs/client/game/profile/list",
                async (url, info, sessionId, output, cancellationToken) => await profileCallbacks.GetMcsBotPlayerProfileForInventoryMode(url, info, sessionId)
            ),
            new RouteAction<McsBotPlayerAidRequestData>(
                "/mcs/client/game/aid/verify",
                async (url, info, sessionId, output, cancellationToken) => await profileCallbacks.VerifyMcsBotPlayerAid(url, info, sessionId)
            ),
            new RouteAction<McsBotPlayerAidRequestData>(
                "/mcs/client/game/aid/remove",
                async (url, info, sessionId, output, cancellationToken) => await profileCallbacks.RemoveMcsBotPlayerAid(url, info, sessionId)
            )
        ]
    )
    { }
}