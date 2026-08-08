using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    /// <summary>
    /// Google Gemini <c>generateContent</c> REST：
    /// <c>POST /v1beta/models/{model}:generateContent?key={ApiKey}</c>。
    /// 意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    public sealed class GoogleGeminiProvider : BaseLlmProvider
    {
        private const string DefaultBaseUrl = "https://generativelanguage.googleapis.com";

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（Gemini API Key）" };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var body = new JObject
            {
                ["system_instruction"] = new JObject { ["parts"] = JArray.FromObject(new[] { new { text = systemPrompt } }) },
                ["contents"] = JArray.FromObject(new[] { new { role = "user", parts = new[] { new { text = userText } } } }),
                ["generationConfig"] = new JObject
                {
                    ["temperature"] = settings.Temperature,
                    ["maxOutputTokens"] = settings.MaxTokens > 0 ? settings.MaxTokens : 3000,
                },
            };

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var content = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (content.StartsWith("Gemini ", StringComparison.Ordinal))
            {
                return new LlmIntent { Error = content };
            }
            return ParseIntentJson(content);
        }

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var body = new JObject
            {
                ["system_instruction"] = new JObject { ["parts"] = JArray.FromObject(new[] { new { text = "You are a connectivity test. Reply with exactly: pong" } }) },
                ["contents"] = JArray.FromObject(new[] { new { role = "user", parts = new[] { new { text = "ping" } } } }),
                ["generationConfig"] = new JObject { ["temperature"] = 0d, ["maxOutputTokens"] = 64 },
            };

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var content = await PostAsync(baseUrl, body, settings, cancellationToken);
            return ExtractText(content) ?? content;
        }

        protected override async Task<string> PostAsync(string baseUrl, JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            
            var model = string.IsNullOrEmpty(settings.ModelId) ? "gemini-2.0-flash" : settings.ModelId;

            var client = AssistantHttpClient.WithTimeout();
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                var endpoint = $"{baseUrl}/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(settings.ApiKey)}";
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"),
                };

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return $"Gemini HTTP {response.StatusCode}: {SafeTrim(responseString, 320)}";
                }
                return responseString;
            }
            catch (OperationCanceledException)
            {
                return "Gemini 请求超时";
            }
            catch (Exception ex)
            {
                return $"Gemini 异常：{ex.Message}";
            }
        }

        private static string ExtractText(string responseString)
        {
            try
            {
                var json = JObject.Parse(responseString);
                var sb = new StringBuilder();
                if (json["candidates"]?[0]?["content"]?["parts"] is JArray parts)
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
