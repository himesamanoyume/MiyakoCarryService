
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
        RaidController raidController
    )
    {
        /// <summary>
        /// 处理 /mcs/client/game/bot/generate
        /// </summary>
        public async ValueTask<string> SpawnMcsBotPlayer(string url, SpawnMcsBotPlayerTypeRequestData info, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.NoBody(await raidController.SpawnMcsBotPlayer(mcsLeadPlayerId, info.Side));
        }

        /// <summary>
        /// 处理 /mcs/singleplayer/settings/bot/get
        /// </summary>
        public async ValueTask<string> GetMcsBotPlayerConfigs(string url, EmptyRequestData _, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.NoBody(await raidController.GetMcsBotPlayerConfigs(mcsLeadPlayerId));
        }

        /// <summary>
        /// 处理 /mcs/singleplayer/settings/bot/upload
        /// </summary>
        public async ValueTask<string> CollectMcsBotPlayerConfig(string url, McsBotPlayerConfigRequestData info, MongoId mcsLeadPlayerId)
        {
            await raidController.CollectMcsBotPlayerConfig(info);
            return httpResponseUtil.NullResponse();
        }

        /// <summary>
        /// 处理 /mcs/singleplayer/info/bot/get
        /// </summary>
        public async ValueTask<string> GetMcsBotPlayerIds(string url, EmptyRequestData _, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.NoBody(raidController.GetMcsBotPlayerIds(mcsLeadPlayerId));
        }

        /// <summary>
        /// 处理 /mcs/singleplayer/info/botids/get
        /// </summary>
        public async ValueTask<string> GetAllMcsBotPlayerIdInRaid(string url, EmptyRequestData _, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.NoBody(raidController.GetAllMcsBotPlayerIdInRaid(mcsLeadPlayerId));
        }
    }
}
