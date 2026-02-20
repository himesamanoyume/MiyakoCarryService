
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record SpawnType
    {
        [JsonPropertyName("WildSpawnType")]
        public required string WildSpawnType { get; set; }

        [JsonPropertyName("IsBoss")]
        public required bool IsBoss { get; set; }

        [JsonPropertyName("DisplayName")]
        public required string DisplayName { get; set; }
    }
}