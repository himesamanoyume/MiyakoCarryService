using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;
using MiyakoCarryService.Server.Models.Providers;
using MiyakoCarryService.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Services.Locales;

namespace MiyakoCarryService.Server.Providers.Llm
{
    [Injectable(InjectionType.Singleton)]
    public sealed class AnthropicProvider : BaseLlmProvider
    {
        public AnthropicProvider(ServerLocalisationService serverLocalisation) : base(serverLocalisation)
        {

        }

        protected override string ProviderDisplayName => _serverLocalisationService.GetText(Locales.LLMPROVIDERANTHROPIC);

        private const string DefaultBaseUrl = "https://api.anthropic.com";
        private const string DefaultModel = "claude-sonnet-4-20250514";
        private const string ApiVersion = "2023-06-01";

        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_USER_TEXT_EMPTY) };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_APIKEY_MISSING, new { ProviderKey = "Anthropic API Key" }) };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;

            var body = new AnthropicMessagesRequest
            {
                Model = model,
                MaxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : 10107,
                System = settings.SystemPrompt ?? "",
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
            ApplyAnthropicThinking(body, settings.ReasoningEffort);

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
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_EMPTY_CONTENT, new { ProviderName = ProviderDisplayName }) };
            }
            return ParseIntentJson(content);
        }

        private void ApplyAnthropicThinking(AnthropicMessagesRequest request, string reasoningEffort)
        {
            if (string.IsNullOrEmpty(reasoningEffort) || reasoningEffort == "default" || reasoningEffort == "none")
            {
                return;
            }

            var budget = reasoningEffort switch
            {
                "low" => 2048,
                "medium" => 4096,
                "high" => 8192,
                "max" => 32000,
                _ => 8192,
            };

            request.Thinking = new AnthropicThinking
            {
                Type = "enabled",
                BudgetTokens = budget,
            };
        }

        public override string ExtractText(string responseString)
        {
            try
            {
                var response = JsonSerializer.Deserialize<AnthropicMessagesResponse>(responseString);
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
            }
            catch
            {
            }
            return null;
        }
    }
}
