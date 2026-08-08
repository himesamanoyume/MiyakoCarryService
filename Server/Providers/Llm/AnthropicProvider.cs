using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Providers.Llm
{
    /// <summary>
    /// Anthropic Claude Messages API <c>/v1/messages</c>。
    /// 鉴权：<c>x-api-key</c> + <c>anthropic-version</c> 头。意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    public sealed class AnthropicProvider : BaseLlmProvider
    {
        private const string DefaultBaseUrl = "https://api.anthropic.com";
        private const string DefaultModel = "claude-sonnet-4-20250514";
        private const string ApiVersion = "2023-06-01";

        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
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
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;

            var body = new JsonObject
            {
                ["model"] = model,
                ["max_tokens"] = settings.MaxTokens > 0 ? settings.MaxTokens : 3000,
                ["system"] = settings.SystemPrompt ?? "",
                ["messages"] = JsonSerializer.SerializeToNode(new[]
                {
                    new { role = "user", content = new[] { new { type = "text", text = userText } } },
                }),
            };

            var result = await PostJsonAsync($"{baseUrl}/v1/messages", body, settings, cancellationToken,
                request =>
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                    request.Headers.Add("x-api-key", settings.ApiKey);
                    request.Headers.Add("anthropic-version", ApiVersion);
                });
            if (!result.IsSuccess)
            {
                return new LlmIntent { Error = result.Error };
            }

            var content = ExtractText(result.ResponseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmIntent { Error = "Anthropic 返回内容为空" };
            }
            return ParseIntentJson(content);
        }

        public override string ExtractText(string responseString)
        {
            try
            {
                var node = JsonNode.Parse(responseString);
                if (node?["content"] is JsonArray content)
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
    }
}
