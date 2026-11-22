using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Match
{
    public record McsMatchGroupStatusResponse
    {
        [JsonPropertyName("players")]
        public List<McsGroupCharacter>? Players { get; set; }

        [JsonPropertyName("maxPveCountExceeded")]
        public bool? MaxPveCountExceeded { get; set; }
    }

    public record McsGroupInviteResponse
    {
        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }

        [JsonPropertyName("invitedMember")]
        public McsGroupCharacter? GroupPlayer { get; set; }
    }
}
