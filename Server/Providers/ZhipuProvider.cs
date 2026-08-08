using System;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Services.Llm.Providers
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

        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }

            var body = BuildChatCompletionsBody(DefaultModel, settings.SystemPrompt, userText, settings.Temperature, settings.MaxTokens);

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

        public Task<PostResponse> PostAsync(JsonObject body, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var jwt = BuildJwt(settings);

            return PostJsonAsync($"{baseUrl}/api/paas/v4/chat/completions", body, settings, cancellationToken,
                request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt));
        }

        private static string BuildJwt(LlmProviderSettings settings)
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
            var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
            var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
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
    }
}
