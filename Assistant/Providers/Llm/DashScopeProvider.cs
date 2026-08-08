using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    /// <summary>
    /// 阿里云 DashScope 通义千问 Chat Completions：
    /// <c>POST /api/v1/services/aigc/text-generation/generation</c>，<c>Authorization: Bearer</c> 鉴权。
    /// 意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    public sealed class DashScopeProvider : BaseLlmProvider
    {
        private const string DefaultBaseUrl = "https://dashscope.aliyuncs.com";
        private const string DefaultModel = "qwen-plus";

        protected override string ProviderTag
        {
            get { return "DashScope"; }
        }

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（DashScope API Key）" };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var messages = new JArray();
            messages.Add(new JObject { ["role"] = "system", ["content"] = systemPrompt });
            messages.Add(new JObject { ["role"] = "user", ["content"] = userText });

            var body = new JObject
            {
                ["model"] = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId,
                ["input"] = new JObject { ["messages"] = messages },
                ["parameters"] = new JObject
                {
                    ["temperature"] = settings.Temperature,
                    ["max_tokens"] = settings.MaxTokens > 0 ? settings.MaxTokens : 3000,
                },
            };

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return new LlmIntent { Error = result.Error };
            }

            var content = ExtractText(result.ResponseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmIntent { Error = "DashScope 返回内容为空" };
            }
            return ParseIntentJson(content);
        }

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var body = new JObject
            {
                ["model"] = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId,
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

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error;
            }
            return ExtractText(result.ResponseText) ?? result.ResponseText;
        }

        private Task<PostResponse> PostAsync(string baseUrl, JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            return SendJsonAsync($"{baseUrl}/api/v1/services/aigc/text-generation/generation", body, settings, cancellationToken,
                request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey));
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
    }
}
