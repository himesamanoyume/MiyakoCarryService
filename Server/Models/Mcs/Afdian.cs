
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Mcs
{
    public record Afdian
    {
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("supporter")]
        public List<string> Supporter { get; set; }
    }
}
