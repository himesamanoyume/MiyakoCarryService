using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record McsPluginClientConfig
    {
        [JsonPropertyName("BalanceRestriction")]
        public bool BalanceRestriction { get; set; } = false;
    }

    public record McsPluginServerConfig
    {
        [JsonPropertyName("CheckUpdate")]
        public bool CheckUpdate { get; set; } = true;

        [JsonPropertyName("CheckIfdian")]
        public bool CheckIfdian { get; set; } = true;

    }

    public record McsPluginConfig
    {
        [JsonPropertyName("Client")]
        public required McsPluginClientConfig ClientConfig { get; set; }

        [JsonPropertyName("Server")]
        public required McsPluginServerConfig ServerConfig { get; set; }
    }

    public record OrderConfig
    {
        [JsonPropertyName("orderQuests")]
        public required List<RepeatableQuestConfig> OrderQuests { get; set; }
    }
}