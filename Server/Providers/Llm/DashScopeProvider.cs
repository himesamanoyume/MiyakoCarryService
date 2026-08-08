using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Providers.Llm
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

        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（DashScope API Key）" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;

            var body = new JsonObject
            {
                ["model"] = model,
                ["input"] = JsonSerializer.SerializeToNode(new
                {
                    messages = new[]
                    {
                        new { role = "system", content = settings.SystemPrompt ?? "" },
                        new { role = "user", content = userText },
                    },
                }),
                ["parameters"] = JsonSerializer.SerializeToNode(new
                {
                    temperature = settings.Temperature,
                    max_tokens = settings.MaxTokens > 0 ? settings.MaxTokens : 3000,
                }),
            };

            var result = await PostJsonAsync($"{baseUrl}/api/v1/services/aigc/text-generation/generation", body, settings, cancellationToken,
                request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey));
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

        public override string ExtractText(string responseString)
        {
            try
            {
                var node = JsonNode.Parse(responseString);
                return node?["output"]?["text"]?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
