
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
        public void AddTraderStanding(MongoId mcsLeadPlayerId, double dif)
        {
            traderService.AddTraderStanding(mcsLeadPlayerId, dif);
        }
    }
}