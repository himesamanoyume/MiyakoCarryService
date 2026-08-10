namespace MiyakoCarryService.Assistant.Models
{
    public sealed class ProviderSettings
    {
        public string ApiKey;
        public string ApiSecret;
        public string BaseUrl;
        public string ModelId;
        public string Language;
        public string SystemPrompt;
        public double Temperature = 0.2;
        public int MaxTokens = 3000;
        public int TimeoutSec = 15;
        public string ReasoningEffort;
    }
}