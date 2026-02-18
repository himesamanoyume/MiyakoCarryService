
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Models.Eft.Trader;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public sealed class TraderCallbacks(
        HttpResponseUtil httpResponseUtil,
        TraderController traderController
    )
    {
        /// <summary>
        /// 处理 /mcs/client/trading/api/friendlyFirePenalty
        /// </summary>
        public ValueTask<string> FriendlyFirePenalty(string url, FriendlyFirePenaltyRequestData info, MongoId mcsLeadPlayerId)
        {
            traderController.FriendlyFirePenalty(mcsLeadPlayerId, info);
            return new ValueTask<string>(httpResponseUtil.NoBody(true));
        }
    }
}