using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
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
    /// 智谱 GLM Chat Completions：<c>POST /api/paas/v4/chat/completions</c>。
    /// 鉴权为 JWT(HS256)：ApiKey 可填完整 "{id}.{secret}"，或 ApiKey=id + ApiSecret=secret。
    /// 意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    internal sealed class ZhipuProvider : ILlmProvider
    {
        private const string DefaultBaseUrl = "https://open.bigmodel.cn";

        public async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var body = BuildBody(systemPrompt, userText, settings.MaxTokens, settings.Temperature);

            var content = await PostAsync(body, settings, cancellationToken);
            if (content.StartsWith("Zhipu ", StringComparison.Ordinal))
            {
                return new LlmIntent { Error = content };
            }
            return OpenAICompatibleProvider.ParseIntentJson(content);
        }

        public async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var body = BuildBody("You are a connectivity test. Reply with exactly: pong", "ping", 64, 0d);
            var content = await PostAsync(body, settings, cancellationToken);
            return ExtractText(content) ?? content;
        }

        private static JObject BuildBody(string systemPrompt, string userText, int maxTokens, double temperature)
        {
            var messages = new JArray();
            messages.Add(new JObject { ["role"] = "system", ["content"] = systemPrompt });
            messages.Add(new JObject { ["role"] = "user", ["content"] = userText });
            return new JObject
            {
                ["model"] = "glm-4-flash",
                ["messages"] = messages,
                ["temperature"] = temperature,
                ["max_tokens"] = maxTokens > 0 ? maxTokens : 3000,
            };
        }

        private async Task<string> PostAsync(JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var jwt = BuildJwt(settings);

            var client = AssistantHttpClient.WithTimeout();
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                var endpoint = $"{baseUrl}/api/paas/v4/chat/completions";
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"),
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return $"Zhipu HTTP {response.StatusCode}: {SafeTrim(responseString, 320)}";
                }
                return responseString;
            }
            catch (OperationCanceledException)
            {
                return "Zhipu 请求超时";
            }
            catch (Exception ex)
            {
                return $"Zhipu 异常：{ex.Message}";
            }
        }

        private static string BuildJwt(ProviderSettings settings)
        {
            var apiKey = settings?.ApiKey ?? string.Empty;
            var apiSecret = settings?.ApiSecret ?? string.Empty;
            if (!string.IsNullOrEmpty(apiKey))
            {
                var dot = apiKey.IndexOf('.');
                if (dot > 0 && string.IsNullOrEmpty(apiSecret))
                {
                    apiSecret = apiKey.Substring(dot + 1);
                    apiKey = apiKey.Substring(0, dot);
                }
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var header = Base64Url(JsonConvert.SerializeObject(new { alg = "HS256", typ = "JWT" }));
            var payload = Base64Url(JsonConvert.SerializeObject(new
            {
                api_key = new { api_key = apiKey, api_secret = apiSecret },
                exp = now + 3600,
                iat = now,
                timestamp = now,
            }));

            var signingInput = $"{header}.{payload}";
            byte[] signature;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret)))
            {
                signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput));
            }
            return $"{signingInput}.{Base64Url(signature)}";
        }

        private static string Base64Url(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        private static string Base64Url(string s) => Base64Url(Encoding.UTF8.GetBytes(s));

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
