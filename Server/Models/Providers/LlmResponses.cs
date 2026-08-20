using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Providers
{
    public sealed record OpenAiChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice> Choices { get; set; }

        [JsonPropertyName("base_resp")]
        public MiniMaxBaseResp BaseResp { get; set; }
    }

    public sealed record OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiChoiceMessage Message { get; set; }
    }

    public sealed record OpenAiChoiceMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public sealed record MiniMaxBaseResp
    {
        [JsonPropertyName("status_code")]
        public int? StatusCode { get; set; }

        [JsonPropertyName("status_msg")]
        public string StatusMsg { get; set; }
    }

    public sealed record AnthropicMessagesResponse
    {
        [JsonPropertyName("content")]
        public List<AnthropicTextContent> Content { get; set; }
    }

    public sealed record DashScopeGenerationResponse
    {
        [JsonPropertyName("output")]
        public DashScopeOutput Output { get; set; }
    }

    public sealed record DashScopeOutput
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public sealed record GeminiGenerateContentResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate> Candidates { get; set; }
    }

    public sealed record GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiCandidateContent Content { get; set; }
    }

    public sealed record GeminiCandidateContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; }
    }
}