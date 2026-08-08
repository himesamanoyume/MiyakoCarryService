using System;
using System.Net.Http;
using System.Net.Http.Headers;
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
    /// Anthropic Claude Messages API <c>/v1/messages</c>。
    /// 鉴权：<c>x-api-key</c> + <c>anthropic-version</c> 头。意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    internal sealed class AnthropicProvider : ILlmProvider
    {
        private const string DefaultBaseUrl = "https://api.anthropic.com";
        private const string ApiVersion = "2023-06-01";

        public async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（Anthropic API Key）" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? "claude-sonnet-4-20250514" : settings.ModelId;
            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);

            var body = new JObject
            {
                ["model"] = model,
                ["max_tokens"] = settings.MaxTokens > 0 ? settings.MaxTokens : 3000,
                ["system"] = systemPrompt,
                ["messages"] = JArray.FromObject(new[]
                {
                    new { role = "user", content = new[] { new { type = "text", text = userText } } },
                }),
            };

            return await SendAndParseAsync(baseUrl, body, settings, cancellationToken);
        }

        public async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? "claude-sonnet-4-20250514" : settings.ModelId;

            var body = new JObject
            {
                ["model"] = model,
                ["max_tokens"] = 64,
                ["system"] = "You are a connectivity test. Reply with exactly: pong",
                ["messages"] = JArray.FromObject(new[]
                {
                    new { role = "user", content = new[] { new { type = "text", text = "ping" } } },
                }),
            };

            var reply = await PostAsync(baseUrl, body, settings, cancellationToken);
            return ExtractText(reply) ?? reply;
        }

        private async Task<LlmIntent> SendAndParseAsync(string baseUrl, JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            var content = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (content.StartsWith("Anthropic ", StringComparison.Ordinal))
            {
                return new LlmIntent { Error = content };
            }
            return OpenAICompatibleProvider.ParseIntentJson(content);
        }

        private async Task<string> PostAsync(string baseUrl, JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            var client = AssistantHttpClient.WithTimeout();
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/messages")
                {
                    Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"),
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                request.Headers.Add("x-api-key", settings.ApiKey);
                request.Headers.Add("anthropic-version", ApiVersion);

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return $"Anthropic HTTP {response.StatusCode}: {SafeTrim(responseString, 320)}";
                }
                return responseString;
            }
            catch (OperationCanceledException)
            {
                return "Anthropic 请求超时";
            }
            catch (Exception ex)
            {
                return $"Anthropic 异常：{ex.Message}";
            }
        }

        private string ExtractText(string responseString)
        {
            try
            {
                var json = JObject.Parse(responseString);
                if (json["content"] is JArray content)
                {
                    var sb = new StringBuilder();
                    foreach (var item in content)
                    {
                        if (item?["type"]?.ToString() == "text")
                        {
                            sb.Append(item["text"]?.ToString());
                        }
                    }
                    return sb.ToString();
                }
            }
            catch
            {
            }
            return null;
        }

        private string SafeTrim(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) { return string.Empty; }
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }
    }
}
