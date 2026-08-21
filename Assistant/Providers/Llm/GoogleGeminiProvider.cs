using System;
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
    public sealed class GoogleGeminiProvider : BaseLlmProvider
    {
        protected override string ProviderDisplayName => Locales.LLMPROVIDERGOOGLEGEMINI.McsLocalized();

        private const string DefaultBaseUrl = "https://generativelanguage.googleapis.com";
        private const string DefaultModel = "gemini-2.0-flash";

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = Locales.LLM_USER_TEXT_EMPTY.McsLocalized() };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = string.Format(Locales.LLM_APIKEY_MISSING.McsLocalized(), "Gemini API Key") };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var body = new GeminiGenerateContentRequest
            {
                SystemInstruction = new GeminiPartList
                {
                    Parts = [new GeminiPart { Text = systemPrompt }],
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
            ApplyGeminiThinking(body, settings.ReasoningEffort);

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
            var body = new GeminiGenerateContentRequest
            {
                SystemInstruction = new GeminiPartList
                {
                    Parts = [new GeminiPart { Text = "You are a connectivity test. Reply with exactly: pong" }],
                },
                Contents =
                [
                    new GeminiContent
                    {
                        Role = "user",
                        Parts = [new GeminiPart { Text = "ping" }],
                    },
                ],
                GenerationConfig = new GeminiGenerationConfig { Temperature = 0d, MaxOutputTokens = 64 },
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
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var endpoint = $"{baseUrl}/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(settings.ApiKey)}";
            return SendJsonAsync(endpoint, body, settings, cancellationToken);
        }

        private void ApplyGeminiThinking(GeminiGenerateContentRequest request, string reasoningEffort)
        {
            if (string.IsNullOrEmpty(reasoningEffort) || reasoningEffort == "default")
            {
                return;
            }

            var budget = reasoningEffort switch
            {
                "none" => 0,
                "low" => 1024,
                "medium" => 4096,
                "high" => 8192,
                "max" => 16384,
                _ => 8192,
            };

            request.GenerationConfig.ThinkingConfig = new GeminiThinkingConfig
            {
                ThinkingBudget = budget,
            };
        }

        private string ExtractText(string responseString)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<GeminiGenerateContentResponse>(responseString);
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
