
using MiyakoCarryService.Server.Callbacks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
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
            new RouteAction<SpawnMcsBotPlayerTypeRequestData>(
                "/mcs/client/game/bot/generate",
                async (url, info, sessionId, output) => await botCallbacks.SpawnMcsBotPlayer(url, info, sessionId)
            )
        ]
    )
    { }
}