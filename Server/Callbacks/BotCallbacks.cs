
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public sealed class BotCallbacks(
        HttpResponseUtil httpResponseUtil,
        BotController botController
    )
    {
        /// <summary>
        /// 处理 /mcs/client/game/bot/generate
        /// </summary>
        /// <returns></returns>
        public async ValueTask<string> SpawnCarryServicePlayer(string url, EmptyRequestData _, MongoId mcsBossPlayerId)
        {
            return httpResponseUtil.NoBody(await botController.SpawnMcsBotPlayer(mcsBossPlayerId));
        }
    }
}
