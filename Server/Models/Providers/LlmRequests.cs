using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Providers
{
    public sealed record OpenAiChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<OpenAiChatMessage> Messages { get; set; }

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("tokens_to_generate")]
        public int? TokensToGenerate { get; set; }

        [JsonPropertyName("reasoning_effort")]
        public string ReasoningEffort { get; set; }

        [JsonPropertyName("thinking")]
        public OpenAiThinking Thinking { get; set; }

        [JsonPropertyName("response_format")]
        public OpenAiResponseFormat ResponseFormat { get; set; }
    }

    public sealed record OpenAiThinking
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public sealed record OpenAiChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public sealed record OpenAiResponseFormat
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public sealed record AnthropicMessagesRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("system")]
        public string System { get; set; }

        [JsonPropertyName("messages")]
        public List<AnthropicMessage> Messages { get; set; }

        [JsonPropertyName("thinking")]
        public AnthropicThinking Thinking { get; set; }
    }

    public sealed record AnthropicThinking
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("budget_tokens")]
        public int? BudgetTokens { get; set; }
    }

    public sealed record AnthropicMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public List<AnthropicTextContent> Content { get; set; }
    }

    public sealed record AnthropicTextContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public sealed record DashScopeGenerationRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("input")]
        public DashScopeInput Input { get; set; }

        [JsonPropertyName("parameters")]
        public DashScopeParameters Parameters { get; set; }
    }

    public sealed record DashScopeInput
    {
        [JsonPropertyName("messages")]
        public List<OpenAiChatMessage> Messages { get; set; }
    }

    public sealed record DashScopeParameters
    {
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("enable_thinking")]
        public bool? EnableThinking { get; set; }
    }

    public sealed record GeminiGenerateContentRequest
    {
        [JsonPropertyName("system_instruction")]
        public GeminiPartList SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; }

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig GenerationConfig { get; set; }
    }

    public sealed record GeminiContent
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; }
    }

    public sealed record GeminiPartList
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; }
    }

    public sealed record GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public sealed record GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("maxOutputTokens")]
        public int? MaxOutputTokens { get; set; }

        [JsonPropertyName("thinkingConfig")]
        public GeminiThinkingConfig ThinkingConfig { get; set; }
    }

    public sealed record GeminiThinkingConfig
    {
        [JsonPropertyName("thinkingBudget")]
        public int? ThinkingBudget { get; set; }
    }
}