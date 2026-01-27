
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
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
        public async ValueTask<string> SpawnMcsBotPlayer(string url, EmptyRequestData _, MongoId mcsBossPlayerId)
        {
            return httpResponseUtil.NoBody(await botController.SpawnMcsBotPlayer(mcsBossPlayerId));
        }

        /// <summary>
        /// 处理 /mcs/singleplayer/settings/bot/get
        /// </summary>
        public async ValueTask<string> GetMcsBotPlayerConfigs(string url, EmptyRequestData _, MongoId mcsBossPlayerId)
        {
            return httpResponseUtil.NoBody(await botController.GetMcsBotPlayerConfigs(mcsBossPlayerId));
        }

        /// <summary>
        /// 处理 /mcs/singleplayer/settings/bot/upload
        /// </summary>
        public async ValueTask<string> CollectMcsBotPlayerConfig(string url, McsBotPlayerConfigRequestData info, MongoId mcsBossPlayerId)
        {
            await botController.CollectMcsBotPlayerConfig(info);
            return httpResponseUtil.NullResponse();
        }
    }
}
