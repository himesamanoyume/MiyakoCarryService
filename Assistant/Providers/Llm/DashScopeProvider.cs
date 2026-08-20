using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Models.Providers;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;
using Newtonsoft.Json;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    public sealed class DashScopeProvider : BaseLlmProvider
    {
        protected override string ProviderDisplayName => Locales.LLMPROVIDERDASHSCOPE.McsLocalized();

        private const string DefaultBaseUrl = "https://dashscope.aliyuncs.com";
        private const string DefaultModel = "qwen-plus";

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = Locales.LLM_USER_TEXT_EMPTY.McsLocalized() };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = string.Format(Locales.LLM_APIKEY_MISSING.McsLocalized(), "DashScope API Key") };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var body = new DashScopeGenerationRequest
            {
                Model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId,
                Input = new DashScopeInput
                {
                    Messages =
                    [
                        new OpenAiChatMessage { Role = "system", Content = systemPrompt },
                        new OpenAiChatMessage { Role = "user", Content = userText },
                    ],
                },
                Parameters = new DashScopeParameters
                {
                    Temperature = settings.Temperature,
                    MaxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : 10107,
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
                return new LlmIntent { Error = string.Format(Locales.LLM_EMPTY_CONTENT.McsLocalized(), ProviderDisplayName) };
            }
            return ParseIntentJson(content);
        }

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var body = new DashScopeGenerationRequest
            {
                Model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId,
                Input = new DashScopeInput
                {
                    Messages =
                    [
                        new OpenAiChatMessage { Role = "system", Content = "You are a connectivity test. Reply with exactly: pong" },
                        new OpenAiChatMessage { Role = "user", Content = "ping" },
                    ],
                },
                Parameters = new DashScopeParameters { Temperature = 0d, MaxTokens = 64 },
            };

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error;
            }
            return ExtractText(result.ResponseText) ?? result.ResponseText;
        }

        public override Task<PostResponse> PostAsync(string baseUrl, object body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            return SendJsonAsync($"{baseUrl}/api/v1/services/aigc/text-generation/generation", body, settings, cancellationToken, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey));
        }

        private string ExtractText(string responseString)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<DashScopeGenerationResponse>(responseString);
                return response?.Output?.Text;
            }
            catch
            {
                return null;
            }
        }
    }
}
