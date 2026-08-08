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
    /// 讯飞星火 OpenAI 兼容 HTTP 端点：<c>POST /v1/chat/completions</c>。
    /// 鉴权：Bearer 为星火 APIKey（新版形如 "{apikey}:{apisecret}"）；若 ApiSecret 已单独填写则拼接 "{ApiKey}:{ApiSecret}"。
    /// 意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    internal sealed class SparkProvider : ILlmProvider
    {
        private const string DefaultBaseUrl = "https://spark-api-open.xf-yun.com";

        public async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（星火 APIKey）" };
            }

            var systemPrompt = PromptTemplates.BuildSystemPrompt(settings.SystemPrompt);
            var body = BuildBody(settings, systemPrompt, userText, settings.MaxTokens, settings.Temperature);

            var content = await PostAsync(body, settings, cancellationToken);
            if (content.StartsWith("Spark ", StringComparison.Ordinal))
            {
                return new LlmIntent { Error = content };
            }
            return OpenAICompatibleProvider.ParseIntentJson(content);
        }

        public async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var body = BuildBody(settings, "You are a connectivity test. Reply with exactly: pong", "ping", 64, 0d);
            var content = await PostAsync(body, settings, cancellationToken);
            return ExtractText(content) ?? content;
        }

        private static JObject BuildBody(ProviderSettings settings, string systemPrompt, string userText, int maxTokens, double temperature)
        {
            var messages = new JArray();
            messages.Add(new JObject { ["role"] = "system", ["content"] = systemPrompt });
            messages.Add(new JObject { ["role"] = "user", ["content"] = userText });
            return new JObject
            {
                ["model"] = string.IsNullOrEmpty(settings.ModelId) ? "generalv3.5" : settings.ModelId,
                ["messages"] = messages,
                ["temperature"] = temperature,
                ["max_tokens"] = maxTokens > 0 ? maxTokens : 3000,
            };
        }

        private async Task<string> PostAsync(JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var bearer = string.IsNullOrEmpty(settings.ApiSecret) ? settings.ApiKey : $"{settings.ApiKey}:{settings.ApiSecret}";

            var client = AssistantHttpClient.WithTimeout(settings);
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                var endpoint = $"{baseUrl}/v1/chat/completions";
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"),
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return $"Spark HTTP {response.StatusCode}: {SafeTrim(responseString, 320)}";
                }
                return responseString;
            }
            catch (OperationCanceledException)
            {
                return "Spark 请求超时";
            }
            catch (Exception ex)
            {
                return $"Spark 异常：{ex.Message}";
            }
        }

        private static string ExtractText(string responseString)
        {
            try
            {
                var json = JObject.Parse(responseString);
                return json["choices"]?[0]?["message"]?["content"]?.ToString();
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
