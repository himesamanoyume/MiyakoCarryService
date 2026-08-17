using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;
using MiyakoCarryService.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Services.Locales;

namespace MiyakoCarryService.Server.Providers.Llm
{
    [Injectable(InjectionType.Singleton)]
    public sealed class MiniMaxProvider : BaseLlmProvider
    {
        public MiniMaxProvider(ServerLocalisationService serverLocalisation) : base(serverLocalisation)
        {
        }

        protected override string ProviderDisplayName => _serverLocalisationService.GetText(Locales.LLMPROVIDERMINIMAX);

        private const string DefaultBaseUrl = "https://api.minimax.chat";
        private const string DefaultModel = "MiniMax-Text-01";

        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_USER_TEXT_EMPTY) };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_APIKEY_MISSING, new { ProviderKey = "MiniMax API Key" }) };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, settings.SystemPrompt, userText, settings.Temperature, settings.MaxTokens, maxTokensFieldName: "tokens_to_generate");

            var result = await PostJsonAsync($"{baseUrl}/v2/text/chat_completions", body, settings, cancellationToken,
                request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey));
            if (!result.IsSuccess)
            {
                return new LlmIntent { Error = result.Error };
            }

            var businessError = CheckBusinessError(result.ResponseText);
            if (businessError != null)
            {
                return new LlmIntent { Error = businessError };
            }

            var content = ExtractChatContentText(result.ResponseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_EMPTY_CONTENT, new { ProviderName = ProviderDisplayName }) };
            }
            return ParseIntentJson(content);
        }

        private string CheckBusinessError(string responseString)
        {
            try
            {
                var node = JsonNode.Parse(responseString);
                var statusCode = node?["base_resp"]?["status_code"]?.GetValue<int>() ?? 0;
                if (statusCode != 0)
                {
                    var statusMsg = node?["base_resp"]?["status_msg"]?.ToString() ?? string.Empty;
                    return $"MiniMax 业务错误 {statusCode}: {SafeTrim(statusMsg, 240)}";
                }
            }
            catch
            {
                
            }
            return null;
        }
    }
}
