
using MiyakoCarryService.Server.Callbacks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Routers.Static
{
    [Injectable]
    public class BotStaticRouter(
        JsonUtil jsonUtil,
        BotCallbacks botCallbacks
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<McsBotPlayerTypeRequestData>(
                "/mcs/client/game/bot/generate",
                async (url, info, sessionId, output, cancellationToken) => await botCallbacks.SpawnMcsBotPlayer(url, info, sessionId)
            ),
            new RouteAction<EmptyRequestData>(
                "/mcs/singleplayer/settings/bot/get",
                async (url, info, sessionId, output, cancellationToken) => await botCallbacks.GetMcsBotPlayerConfigs(url, info, sessionId)
            ),
            new RouteAction<McsBotPlayerConfigRequestData>(
                "/mcs/singleplayer/settings/bot/upload",
                async (url, info, sessionId, output, cancellationToken) => await botCallbacks.CollectMcsBotPlayerConfig(url, info, sessionId)
            ),
            new RouteAction<McsBotPlayerTypeRequestData>(
                "/mcs/singleplayer/info/bot/get",
                async (url, info, sessionId, output, cancellationToken) => await botCallbacks.GetMcsBotPlayerIds(url, info, sessionId)
            ),
            new RouteAction<McsBotPlayerTypeRequestData>(
                "/mcs/singleplayer/info/botids/get",
                async (url, info, sessionId, output, cancellationToken) => await botCallbacks.GetAllMcsBotPlayerIdInRaid(url, info, sessionId)
            )
        ]
    )
    { }
}