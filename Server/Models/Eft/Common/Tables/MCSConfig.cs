using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record McsClientConfig
    {

    }

    public record McsServerConfig
    {

    }

    public record McsConfig
    {
        [JsonPropertyName("Client")]
        public required McsClientConfig ClientConfig { get; set; }

        [JsonPropertyName("Server")]
        public required McsServerConfig ServerConfig { get; set; }
    }

    public record OrderConfig
    {
        [JsonPropertyName("orderQuests")]
        public required List<RepeatableQuestConfig> OrderQuests { get; set; }
    }
}