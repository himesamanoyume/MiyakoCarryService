using System.Collections.Generic;
using Newtonsoft.Json;

namespace MiyakoCarryService.Assistant.Models.Providers
{
    public sealed class OpenAiChatRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("messages")]
        public List<OpenAiChatMessage> Messages { get; set; }

        [JsonProperty("temperature")]
        public double? Temperature { get; set; }

        [JsonProperty("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonProperty("tokens_to_generate")]
        public int? TokensToGenerate { get; set; }

        [JsonProperty("reasoning_effort")]
        public string ReasoningEffort { get; set; }

        [JsonProperty("thinking")]
        public OpenAiThinking Thinking { get; set; }

        [JsonProperty("response_format")]
        public OpenAiResponseFormat ResponseFormat { get; set; }
    }

    public sealed class OpenAiThinking
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public sealed class OpenAiChatMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public sealed class OpenAiResponseFormat
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    /// <summary>
    /// Anthropic Messages API 请求体。
    /// </summary>
    public sealed class AnthropicMessagesRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonProperty("system")]
        public string System { get; set; }

        [JsonProperty("messages")]
        public List<AnthropicMessage> Messages { get; set; }

        [JsonProperty("thinking")]
        public AnthropicThinking Thinking { get; set; }
    }

    public sealed class AnthropicThinking
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("budget_tokens")]
        public int? BudgetTokens { get; set; }
    }

    public sealed class AnthropicMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public List<AnthropicTextContent> Content { get; set; }
    }

    public sealed class AnthropicTextContent
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    /// <summary>
    /// 阿里云 DashScope 文本生成请求体。
    /// </summary>
    public sealed class DashScopeGenerationRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("input")]
        public DashScopeInput Input { get; set; }

        [JsonProperty("parameters")]
        public DashScopeParameters Parameters { get; set; }
    }

    public sealed class DashScopeInput
    {
        [JsonProperty("messages")]
        public List<OpenAiChatMessage> Messages { get; set; }
    }

    public sealed class DashScopeParameters
    {
        [JsonProperty("temperature")]
        public double? Temperature { get; set; }

        [JsonProperty("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonProperty("enable_thinking")]
        public bool? EnableThinking { get; set; }
    }

    /// <summary>
    /// Google Gemini generateContent 请求体。
    /// </summary>
    public sealed class GeminiGenerateContentRequest
    {
        [JsonProperty("system_instruction")]
        public GeminiPartList SystemInstruction { get; set; }

        [JsonProperty("contents")]
        public List<GeminiContent> Contents { get; set; }

        [JsonProperty("generationConfig")]
        public GeminiGenerationConfig GenerationConfig { get; set; }
    }

    public sealed class GeminiContent
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("parts")]
        public List<GeminiPart> Parts { get; set; }
    }

    public sealed class GeminiPartList
    {
        [JsonProperty("parts")]
        public List<GeminiPart> Parts { get; set; }
    }

    public sealed class GeminiPart
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public sealed class GeminiGenerationConfig
    {
        [JsonProperty("temperature")]
        public double? Temperature { get; set; }

        [JsonProperty("maxOutputTokens")]
        public int? MaxOutputTokens { get; set; }

        [JsonProperty("thinkingConfig")]
        public GeminiThinkingConfig ThinkingConfig { get; set; }
    }

    public sealed class GeminiThinkingConfig
    {
        [JsonProperty("thinkingBudget")]
        public int? ThinkingBudget { get; set; }
    }
}