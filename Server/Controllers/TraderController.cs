
using MiyakoCarryService.Server.Models.Eft.Trader;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class TraderController(
        TraderService traderService
    )
    {
        public void FriendlyFirePenalty(MongoId mcsLeadPlayerId, FriendlyFirePenaltyRequestData info)
        {
            traderService.FriendlyFirePenalty(mcsLeadPlayerId, info);
        }
    }
}