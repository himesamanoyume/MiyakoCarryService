using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Models.Providers;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;
using Newtonsoft.Json;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
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

            var body = new AnthropicMessagesRequest
            {
                Model = model,
                MaxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : 10107,
                System = systemPrompt,
                Messages =
                [
                    new AnthropicMessage
                    {
                        Role = "user",
                        Content =
                        [
                            new AnthropicTextContent { Type = "text", Text = userText },
                        ],
                    },
                ],
            };

            return await SendAndParseAsync(baseUrl, body, settings, cancellationToken);
        }

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;

            var body = new AnthropicMessagesRequest
            {
                Model = model,
                MaxTokens = 64,
                System = "You are a connectivity test. Reply with exactly: pong",
                Messages =
                [
                    new AnthropicMessage
                    {
                        Role = "user",
                        Content =
                        [
                            new AnthropicTextContent { Type = "text", Text = "ping" },
                        ],
                    },
                ],
            };

            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error;
            }
            return ExtractText(result.ResponseText) ?? result.ResponseText;
        }

        private async Task<LlmIntent> SendAndParseAsync(string baseUrl, AnthropicMessagesRequest body, ProviderSettings settings, CancellationToken cancellationToken)
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

        public override Task<PostResponse> PostAsync(string baseUrl, object body, ProviderSettings settings, CancellationToken cancellationToken)
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
                var response = JsonConvert.DeserializeObject<AnthropicMessagesResponse>(responseString);
                if (response?.Content is { Count: > 0 })
                {
                    var sb = new StringBuilder();
                    foreach (var item in response.Content)
                    {
                        if (item?.Type == "text")
                        {
                            sb.Append(item.Text);
                        }
                    }
                    return sb.ToString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
