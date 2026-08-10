using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Llm
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

        protected override string ProviderDisplayName => Locales.LLMPROVIDERANTHROPIC.McsLocalized();

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = Locales.LLM_USER_TEXT_EMPTY.McsLocalized() };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = string.Format(Locales.LLM_APIKEY_MISSING.McsLocalized(), "Anthropic API Key") };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
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

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;

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

            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error;
            }
            return ExtractText(result.ResponseText) ?? result.ResponseText;
        }

        private async Task<LlmIntent> SendAndParseAsync(string baseUrl, JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return new LlmIntent { Error = result.Error };
            }

            var content = ExtractText(result.ResponseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmIntent { Error = string.Format(Locales.LLM_EMPTY_CONTENT.McsLocalized(), ProviderDisplayName) };
            }
            return ParseIntentJson(content);
        }

        public override Task<PostResponse> PostAsync(string baseUrl, JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            return SendJsonAsync($"{baseUrl}/v1/messages", body, settings, cancellationToken,
                request =>
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                    request.Headers.Add("x-api-key", settings.ApiKey);
                    request.Headers.Add("anthropic-version", ApiVersion);
                });
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
    }
}
