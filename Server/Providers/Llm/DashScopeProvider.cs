using System.Net.Http.Headers;
using System.Text.Json;
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
    public sealed class DashScopeProvider : BaseLlmProvider
    {
        public DashScopeProvider(ServerLocalisationService serverLocalisation) : base(serverLocalisation)
        {
        }

        protected override string ProviderDisplayName => _serverLocalisationService.GetText(Locales.LLMPROVIDERDASHSCOPE);

        private const string DefaultBaseUrl = "https://dashscope.aliyuncs.com";
        private const string DefaultModel = "qwen-plus";

        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_USER_TEXT_EMPTY) };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_APIKEY_MISSING, new { ProviderKey = "DashScope API Key" }) };
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
                    max_tokens = settings.MaxTokens > 0 ? settings.MaxTokens : 10107,
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
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_EMPTY_CONTENT, new { ProviderName = ProviderDisplayName }) };
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
