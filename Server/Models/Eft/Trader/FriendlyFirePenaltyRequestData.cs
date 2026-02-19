
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Trader
{
    public record FriendlyFirePenaltyRequestData : IRequestData
    {
        [JsonPropertyName("FriendlyFireLeadPlayerId")]
        public required MongoId FriendlyFireLeadPlayerId { get; set; }
        
        [JsonPropertyName("Diff")]
        public required double Diff { get; set; }

        [JsonPropertyName("TeamKill")]
        public required bool TeamKill { get; set; }
    }
}