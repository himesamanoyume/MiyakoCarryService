using System;
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
    public sealed class ZhipuProvider : BaseLlmProvider
    {
        private const string DefaultBaseUrl = "https://open.bigmodel.cn";
        private const string DefaultModel = "glm-4-flash";

        protected override string ProviderTag
        {
            get { return "Zhipu"; }
        }

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var body = BuildChatCompletionsBody(DefaultModel, systemPrompt, userText, settings.Temperature, settings.MaxTokens);

            var result = await PostAsync(body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return new LlmIntent { Error = result.Error };
            }

            var content = ExtractChatContentText(result.ResponseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmIntent { Error = "Zhipu 返回内容为空" };
            }
            return ParseIntentJson(content);
        }

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var body = BuildChatCompletionsBody(DefaultModel, "You are a connectivity test. Reply with exactly: pong", "ping", 0d, 64);
            var result = await PostAsync(body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error;
            }
            return ExtractChatContentText(result.ResponseText) ?? result.ResponseText;
        }

        private Task<PostResponse> PostAsync(JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var jwt = BuildJwt(settings);

            return SendJsonAsync($"{baseUrl}/api/paas/v4/chat/completions", body, settings, cancellationToken,
                request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt));
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

        private static string Base64Url(byte[] data)
        {
            return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string Base64Url(string s)
        {
            return Base64Url(Encoding.UTF8.GetBytes(s));
        }
    }
}
