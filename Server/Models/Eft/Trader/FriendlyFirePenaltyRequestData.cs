
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Trader
{
    public record FriendlyFirePenaltyRequestData : IRequestData
    {
        [JsonPropertyName("FriendlyFireLeadPlayerId")]
        public required MongoId FriendlyFireLeadPlayerId { get; set; }

        [JsonPropertyName("StandingDiff")]
        public required double StandingDiff { get; set; }

        [JsonPropertyName("PunishEveryone")]
        public required bool PunishEveryone { get; set; }
    }
}