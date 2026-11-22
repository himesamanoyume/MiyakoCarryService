using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Trader
{
    public record McsProfileChangesResponse
    {
        [JsonPropertyName("Experience")]
        public int Experience { get; set; }

        [JsonPropertyName("TradersData")]
        public Dictionary<string, McsTraderDataResponse> TradersData { get; set; }
    }

    public record McsTraderDataResponse
    {
        [JsonPropertyName("SalesSum")]
        public long? SalesSum { get; set; }

        [JsonPropertyName("Standing")]
        public double? Standing { get; set; }

        [JsonPropertyName("LoyaltyLevel")]
        public int? LoyaltyLevel { get; set; }

        [JsonPropertyName("Unlocked")]
        public bool? Unlocked { get; set; }

        [JsonPropertyName("Disabled")]
        public bool? Disabled { get; set; }
    }
}
