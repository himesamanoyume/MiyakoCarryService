
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Eft.Ws;

namespace MiyakoCarryService.Server.Models.Eft.Ws
{
    public record McsWsGroupMatchInviteAccept : WsNotificationEvent
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("aid")]
        public int? Aid { get; set; }

        [JsonPropertyName("Info")]
        public CharacterInfo? Info { get; set; }

        [JsonPropertyName("PlayerVisualRepresentation")]
        public PlayerVisualRepresentation? VisualRepresentation { get; set; }

        [JsonPropertyName("isLeader")]
        public bool? IsLeader { get; set; }

        [JsonPropertyName("isReady")]
        public bool? IsReady { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("lookingGroup")]
        public bool? LookingGroup { get; set; }
    }
}