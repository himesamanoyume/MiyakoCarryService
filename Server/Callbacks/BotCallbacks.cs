
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Eft.Bot;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public sealed class BotCallbacks(
        HttpResponseUtil httpResponseUtil,
        
    )
    {
        /// <summary>
        /// 处理 /mcs/client/game/bot/generate
        /// </summary>
        /// <returns></returns>
        public async ValueTask<string> SpawnCarryServicePlayer(string url, SpawnCarryServiceBotRequestData data, MongoId sessionID)
        {
            return httpResponseUtil.GetBody(await bot);
        }
    }
}
