
using MiyakoCarryService.Server.Callbacks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Routers.Static
{
    [Injectable]
    public sealed class BotStaticRouter(
        JsonUtil jsonUtil,
        BotCallbacks botCallbacks
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/mcs/client/game/bot/generate",
                async (url, info, sessionId, output) => await botCallbacks.SpawnMcsBotPlayer(url, info, sessionId)
            )
        ]
    )
    { }
}