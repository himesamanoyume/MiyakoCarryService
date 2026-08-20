using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    public sealed class SparkProvider : BaseLlmProvider
    {
        protected override string ProviderDisplayName => Locales.LLMPROVIDERSPARK.McsLocalized();

        private const string DefaultBaseUrl = "https://spark-api-open.xf-yun.com";
        private const string DefaultModel = "generalv3.5";

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = Locales.LLM_USER_TEXT_EMPTY.McsLocalized() };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = string.Format(Locales.LLM_APIKEY_MISSING.McsLocalized(), "星火 APIKey") };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, systemPrompt, userText, settings.Temperature, settings.MaxTokens);

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return new LlmIntent { Error = result.Error };
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
            var body = BuildChatCompletionsBody(model, "You are a connectivity test. Reply with exactly: pong", "ping", 0d, 64);

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error;
            }
            return ExtractChatContentText(result.ResponseText) ?? result.ResponseText;
        }

        public override Task<PostResponse> PostAsync(string baseUrl, object body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            var bearer = string.IsNullOrEmpty(settings.ApiSecret) ? settings.ApiKey : $"{settings.ApiKey}:{settings.ApiSecret}";
            return SendJsonAsync($"{baseUrl}/v1/chat/completions", body, settings, cancellationToken, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer));
        }
    }
}
