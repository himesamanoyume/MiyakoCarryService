using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record MCSClientConfig
    {

    }

    public record MCSServerConfig
    {

    }

    public record MCSConfig
    {
        [JsonPropertyName("Client")]
        public required MCSClientConfig ClientConfig { get; set; }

        [JsonPropertyName("Server")]
        public required MCSServerConfig ServerConfig { get; set; }
    }

    public record MCSOrderConfig
    {
        [JsonPropertyName("orderQuests")]
        public required List<RepeatableQuestConfig> OrderQuests { get; set; }
    }
}