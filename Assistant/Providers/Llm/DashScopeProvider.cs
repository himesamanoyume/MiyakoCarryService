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
    /// 阿里云 DashScope 通义千问 Chat Completions：
    /// <c>POST /api/v1/services/aigc/text-generation/generation</c>，<c>Authorization: Bearer</c> 鉴权。
    /// 意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    internal sealed class DashScopeProvider : ILlmProvider
    {
        private const string DefaultBaseUrl = "https://dashscope.aliyuncs.com";

        public async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（DashScope API Key）" };
            }

            var systemPrompt = PromptTemplates.BuildSystemPrompt(settings.SystemPrompt);
            var messages = new JArray();
            messages.Add(new JObject { ["role"] = "system", ["content"] = systemPrompt });
            messages.Add(new JObject { ["role"] = "user", ["content"] = userText });

            var body = new JObject
            {
                ["model"] = string.IsNullOrEmpty(settings.ModelId) ? "qwen-plus" : settings.ModelId,
                ["input"] = new JObject { ["messages"] = messages },
                ["parameters"] = new JObject
                {
                    ["temperature"] = settings.Temperature,
                    ["max_tokens"] = settings.MaxTokens > 0 ? settings.MaxTokens : 3000,
                },
            };

            var content = await PostAsync(body, settings, cancellationToken);
            if (content.StartsWith("DashScope ", StringComparison.Ordinal))
            {
                return new LlmIntent { Error = content };
            }
            return OpenAICompatibleProvider.ParseIntentJson(content);
        }

        public async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var body = new JObject
            {
                ["model"] = string.IsNullOrEmpty(settings.ModelId) ? "qwen-plus" : settings.ModelId,
                ["input"] = new JObject
                {
                    ["messages"] = JArray.FromObject(new[]
                    {
                        new { role = "system", content = "You are a connectivity test. Reply with exactly: pong" },
                        new { role = "user", content = "ping" },
                    }),
                },
                ["parameters"] = new JObject { ["temperature"] = 0d, ["max_tokens"] = 64 },
            };

            var content = await PostAsync(body, settings, cancellationToken);
            return ExtractText(content) ?? content;
        }

        private async Task<string> PostAsync(JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');

            var client = AssistantHttpClient.WithTimeout(settings);
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                var endpoint = $"{baseUrl}/api/v1/services/aigc/text-generation/generation";
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"),
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return $"DashScope HTTP {response.StatusCode}: {SafeTrim(responseString, 320)}";
                }
                return responseString;
            }
            catch (OperationCanceledException)
            {
                return "DashScope 请求超时";
            }
            catch (Exception ex)
            {
                return $"DashScope 异常：{ex.Message}";
            }
        }

        private static string ExtractText(string responseString)
        {
            try
            {
                var json = JObject.Parse(responseString);
                return json["output"]?["text"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string SafeTrim(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) { return string.Empty; }
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }
    }
}
