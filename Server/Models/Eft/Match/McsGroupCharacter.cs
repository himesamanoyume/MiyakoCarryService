using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Match;

namespace MiyakoCarryService.Server.Models.Eft.Match
{
    public record McsGroupCharacter
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("aid")]
        public int? Aid { get; set; }

        [JsonPropertyName("Info")]
        public McsCharacterInfo? Info { get; set; }

        [JsonPropertyName("PlayerVisualRepresentation")]
        public McsPlayerVisualRepresentation? VisualRepresentation { get; set; }

        [JsonPropertyName("isLeader")]
        public bool? IsLeader { get; set; }

        [JsonPropertyName("isReady")]
        public bool? IsReady { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("lookingGroup")]
        public bool? LookingGroup { get; set; }
    }

    public record McsCharacterInfo : CharacterInfo
    {
        [JsonPropertyName("Health")]
        public virtual BotBaseHealth? Health { get; set; }
    }

    public record McsPlayerVisualRepresentation
    {
        [JsonPropertyName("Info")]
        public McsVisualInfo? Info { get; set; }

        [JsonPropertyName("Customization")]
        public SPTarkov.Server.Core.Models.Eft.Match.Customization? Customization { get; set; }

        [JsonPropertyName("Equipment")]
        public Equipment? Equipment { get; set; }
    }

    public record McsVisualInfo : VisualInfo
    {
        [JsonPropertyName("Health")]
        public virtual BotBaseHealth? Health { get; set; }
    }
}