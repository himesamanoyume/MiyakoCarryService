using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Providers.Llm
{
    /// <summary>
    /// Google Gemini <c>generateContent</c> REST：
    /// <c>POST /v1beta/models/{model}:generateContent?key={ApiKey}</c>。
    /// 意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    public sealed class GoogleGeminiProvider : BaseLlmProvider
    {
        private const string DefaultBaseUrl = "https://generativelanguage.googleapis.com";
        private const string DefaultModel = "gemini-2.0-flash";

        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（Gemini API Key）" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;

            var body = new JsonObject
            {
                ["system_instruction"] = JsonSerializer.SerializeToNode(new { parts = new[] { new { text = settings.SystemPrompt ?? "" } } }),
                ["contents"] = JsonSerializer.SerializeToNode(new[] { new { role = "user", parts = new[] { new { text = userText } } } }),
                ["generationConfig"] = JsonSerializer.SerializeToNode(new
                {
                    temperature = settings.Temperature,
                    maxOutputTokens = settings.MaxTokens > 0 ? settings.MaxTokens : 3000,
                }),
            };

            var endpoint = $"{baseUrl}/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(settings.ApiKey)}";
            var result = await PostJsonAsync(endpoint, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return new LlmIntent { Error = result.Error };
            }

            var content = ExtractText(result.ResponseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmIntent { Error = "Gemini 返回内容为空" };
            }
            return ParseIntentJson(content);
        }

        public override string ExtractText(string responseString)
        {
            try
            {
                var node = JsonNode.Parse(responseString);
                var sb = new StringBuilder();
                if (node?["candidates"]?[0]?["content"]?["parts"] is JsonArray parts)
                {
                    foreach (var part in parts)
                    {
                        var text = part?["text"]?.ToString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            sb.Append(text);
                        }
                    }
                }
                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
