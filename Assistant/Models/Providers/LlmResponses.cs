using System.Collections.Generic;
using Newtonsoft.Json;

namespace MiyakoCarryService.Assistant.Models.Providers
{
    public sealed class OpenAiChatResponse
    {
        [JsonProperty("choices")]
        public List<OpenAiChoice> Choices { get; set; }

        [JsonProperty("base_resp")]
        public MiniMaxBaseResp BaseResp { get; set; }
    }

    public sealed class OpenAiChoice
    {
        [JsonProperty("message")]
        public OpenAiChoiceMessage Message { get; set; }
    }

    public sealed class OpenAiChoiceMessage
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public sealed class MiniMaxBaseResp
    {
        [JsonProperty("status_code")]
        public int? StatusCode { get; set; }

        [JsonProperty("status_msg")]
        public string StatusMsg { get; set; }
    }

    public sealed class AnthropicMessagesResponse
    {
        [JsonProperty("content")]
        public List<AnthropicTextContent> Content { get; set; }
    }

    public sealed class DashScopeGenerationResponse
    {
        [JsonProperty("output")]
        public DashScopeOutput Output { get; set; }
    }

    public sealed class DashScopeOutput
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public sealed class GeminiGenerateContentResponse
    {
        [JsonProperty("candidates")]
        public List<GeminiCandidate> Candidates { get; set; }
    }

    public sealed class GeminiCandidate
    {
        [JsonProperty("content")]
        public GeminiCandidateContent Content { get; set; }
    }

    public sealed class GeminiCandidateContent
    {
        [JsonProperty("parts")]
        public List<GeminiPart> Parts { get; set; }
    }
}