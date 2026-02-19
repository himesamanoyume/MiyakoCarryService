
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Trader
{
    public record FriendlyFirePenaltyRequestData : IRequestData
    {
        [JsonPropertyName("FriendlyFirePlayerId")]
        public required MongoId FriendlyFirePlayerId { get; set; }
        
        [JsonPropertyName("Diff")]
        public required double Diff { get; set; }

        [JsonPropertyName("TeamKill")]
        public required bool TeamKill { get; set; }

        [JsonPropertyName("PunishEveryone")]
        public required bool PunishEveryone { get; set; }
    }
}