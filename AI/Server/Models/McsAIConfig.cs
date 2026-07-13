using System.Text.Json.Serialization;

namespace MiyakoCarryService.AI.Server.Models
{
    public record McsAIConfig
    {
        [JsonPropertyName("LLMBaseUrl")]
        public string LLMBaseUrl { get; set; } = "";

        [JsonPropertyName("LLMApiKey")]
        public string LLMApiKey { get; set; } = "";
    }
}