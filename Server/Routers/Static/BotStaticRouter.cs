
using MiyakoCarryService.Server.Callbacks;
using MiyakoCarryService.Server.Models.Eft.Bot;
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
            new RouteAction<SpawnCarryServiceBotRequestData>(
                "/mcs/client/game/bot/generate",
                async (url, info, sessionID, output) => await botCallbacks.SpawnCarryServicePlayer(url, info, sessionID)
            )
        ]
    )
    { }
}