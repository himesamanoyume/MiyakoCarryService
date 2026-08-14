using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    public sealed class MiniMaxProvider : BaseLlmProvider
    {
        protected override string ProviderDisplayName => Locales.LLMPROVIDERMINIMAX.McsLocalized();

        private const string DefaultBaseUrl = "https://api.minimax.chat";
        private const string DefaultModel = "MiniMax-Text-01";

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = Locales.LLM_USER_TEXT_EMPTY.McsLocalized() };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = string.Format(Locales.LLM_APIKEY_MISSING.McsLocalized(), "MiniMax API Key") };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, systemPrompt, userText, settings.Temperature, settings.MaxTokens, maxTokensFieldName: "tokens_to_generate");

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
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
                return new LlmIntent { Error = string.Format(Locales.LLM_EMPTY_CONTENT.McsLocalized(), ProviderDisplayName) };
            }
            return ParseIntentJson(content);
        }

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, "You are a connectivity test. Reply with exactly: pong", "ping", 0d, 64, maxTokensFieldName: "tokens_to_generate");

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error;
            }
            return ExtractChatContentText(result.ResponseText) ?? result.ResponseText;
        }

        public override Task<PostResponse> PostAsync(string baseUrl, JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            return SendJsonAsync($"{baseUrl}/v2/text/chat_completions", body, settings, cancellationToken, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey));
        }

        private string CheckBusinessError(string responseString)
        {
            try
            {
                var json = JObject.Parse(responseString);
                var statusCode = json["base_resp"]?["status_code"]?.Value<int>() ?? 0;
                if (statusCode != 0)
                {
                    var statusMsg = json["base_resp"]?["status_msg"]?.ToString() ?? string.Empty;
                    return $"MiniMax Error {statusCode}: {SafeTrim(statusMsg, 240)}";
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
