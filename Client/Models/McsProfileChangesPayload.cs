using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Client.Models
{
    public sealed class McsProfileChangesPayload
    {
        [JsonPropertyName("Experience")]
        public int Experience { get; set; }

        [JsonPropertyName("TradersData")]
        public Dictionary<string, McsTraderDataPayload> TradersData { get; set; }
    }

    public sealed class McsTraderDataPayload
    {
        [JsonPropertyName("SalesSum")]
        public long? SalesSum { get; set; }

        [JsonPropertyName("Standing")]
        public double? Standing { get; set; }

        [JsonPropertyName("LoyaltyLevel")]
        public float? LoyaltyLevel { get; set; }

        [JsonPropertyName("Unlocked")]
        public bool? Unlocked { get; set; }

        [JsonPropertyName("Disabled")]
        public bool? Disabled { get; set; }
    }
}
