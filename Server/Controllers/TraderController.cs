
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Eft.Trader;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class TraderController(
        Services.TraderService traderService
    )
    {
        public void FriendlyFirePenalty(MongoId mcsLeadPlayerId, FriendlyFirePenaltyRequestData info)
        {
            traderService.FriendlyFirePenalty(mcsLeadPlayerId, info);
        }

        public void Compensation(CompensationRequestData info)
        {
            traderService.Compensation(info);
        }

        public async Task<ProfileChange> UpdateProfile(MongoId mcsLeadPlayerId)
        {
            return await traderService.UpdateProfile(mcsLeadPlayerId);
        }

        public TraderAssort GetMcsBotPlayerInventoryModeAssort()
        {
            return traderService.GetMcsBotPlayerInventoryModeAssort();
        }
    }
}