using System;
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
    public sealed class GoogleGeminiProvider : BaseLlmProvider
    {
        public GoogleGeminiProvider(ServerLocalisationService serverLocalisation) : base(serverLocalisation)
        {
        }

        protected override string ProviderDisplayName => _serverLocalisationService.GetText(Locales.LLMPROVIDERGOOGLEGEMINI);

        private const string DefaultBaseUrl = "https://generativelanguage.googleapis.com";
        private const string DefaultModel = "gemini-2.0-flash";

        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_USER_TEXT_EMPTY) };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_APIKEY_MISSING, new { ProviderKey = "Gemini API Key" }) };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;

            var body = new GeminiGenerateContentRequest
            {
                SystemInstruction = new GeminiPartList
                {
                    Parts = [new GeminiPart { Text = settings.SystemPrompt ?? "" }],
                },
                Contents =
                [
                    new GeminiContent
                    {
                        Role = "user",
                        Parts = [new GeminiPart { Text = userText }],
                    },
                ],
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = settings.Temperature,
                    MaxOutputTokens = settings.MaxTokens > 0 ? settings.MaxTokens : 10107,
                },
            };

            var endpoint = $"{baseUrl}/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(settings.ApiKey)}";
            var result = await PostJsonAsync(endpoint, body, settings, cancellationToken);
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
                var response = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(responseString);
                var sb = new StringBuilder();
                if (response?.Candidates is { Count: > 0 })
                {
                    foreach (var candidate in response.Candidates)
                    {
                        if (candidate?.Content?.Parts is { Count: > 0 })
                        {
                            foreach (var part in candidate.Content.Parts)
                            {
                                var text = part?.Text;
                                if (!string.IsNullOrEmpty(text))
                                {
                                    sb.Append(text);
                                }
                            }
                        }
                    }
                }
                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
