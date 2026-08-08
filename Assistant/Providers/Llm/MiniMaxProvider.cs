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
    /// MiniMax v2 Chat Completions：<c>POST /v2/text/chat_completions</c>，Bearer ApiKey 鉴权。
    /// 输出 token 上限用 <c>tokens_to_generate</c>；错误信息在 <c>base_resp</c>。
    /// 意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    public sealed class MiniMaxProvider : BaseLlmProvider
    {
        private const string DefaultBaseUrl = "https://api.minimax.chat";

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（MiniMax API Key）" };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var body = BuildBody(settings, systemPrompt, userText, settings.MaxTokens, settings.Temperature);

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var content = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (content.StartsWith("MiniMax ", StringComparison.Ordinal))
            {
                return new LlmIntent { Error = content };
            }
            return ParseIntentJson(content);
        }

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var body = BuildBody(settings, "You are a connectivity test. Reply with exactly: pong", "ping", 64, 0d);
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var content = await PostAsync(baseUrl, body, settings, cancellationToken);
            return ExtractText(content) ?? content;
        }

        private static JObject BuildBody(ProviderSettings settings, string systemPrompt, string userText, int maxTokens, double temperature)
        {
            var messages = new JArray();
            messages.Add(new JObject { ["role"] = "system", ["content"] = systemPrompt });
            messages.Add(new JObject { ["role"] = "user", ["content"] = userText });
            return new JObject
            {
                ["model"] = string.IsNullOrEmpty(settings.ModelId) ? "MiniMax-Text-01" : settings.ModelId,
                ["messages"] = messages,
                ["temperature"] = temperature,
                ["tokens_to_generate"] = maxTokens > 0 ? maxTokens : 3000,
            };
        }

        protected override async Task<string> PostAsync(string baseUrl, JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            var client = AssistantHttpClient.WithTimeout();
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                var endpoint = $"{baseUrl}/v2/text/chat_completions";
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"),
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return $"MiniMax HTTP {response.StatusCode}: {SafeTrim(responseString, 320)}";
                }

                // v2 业务错误：base_resp.status_code != 0
                var json = JObject.Parse(responseString);
                var statusCode = json["base_resp"]?["status_code"]?.Value<int>() ?? 0;
                if (statusCode != 0)
                {
                    var statusMsg = json["base_resp"]?["status_msg"]?.ToString() ?? string.Empty;
                    return $"MiniMax 业务错误 {statusCode}: {SafeTrim(statusMsg, 240)}";
                }
                return responseString;
            }
            catch (OperationCanceledException)
            {
                return "MiniMax 请求超时";
            }
            catch (Exception ex)
            {
                return $"MiniMax 异常：{ex.Message}";
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
    }
}
