using System;
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
    public sealed class OpenAICompatibleProvider : BaseLlmProvider
    {
        public OpenAICompatibleProvider(ServerLocalisationService serverLocalisation) : base(serverLocalisation)
        {
        }

        protected override string ProviderDisplayName => _serverLocalisationService.GetText(Locales.LLMPROVIDEROPENAICOMPATIBLE);

        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_USER_TEXT_EMPTY) };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? "https://api.deepseek.com" : settings.BaseUrl.TrimEnd('/');
            var modelId = string.IsNullOrEmpty(settings.ModelId) ? "deepseek-v4-flash" : settings.ModelId;
            var maxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : 10107;

            try
            {
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    var useJsonObject = attempt == 0;
                    var useReasoningEffort = attempt == 0 && !string.IsNullOrEmpty(settings.ReasoningEffort) && settings.ReasoningEffort != "default";
                    var body = new JsonObject
                    {
                        ["model"] = modelId,
                        ["messages"] = JsonSerializer.SerializeToNode(new[]
                        {
                            new { role = "system", content = settings.SystemPrompt ?? "" },
                            new { role = "user", content = userText },
                        }),
                        ["temperature"] = settings.Temperature,
                        ["max_tokens"] = maxTokens,
                    };
                    if (useReasoningEffort)
                    {
                        body["reasoning_effort"] = settings.ReasoningEffort;
                    }
                    if (useJsonObject)
                    {
                        body["response_format"] = JsonSerializer.SerializeToNode(new { type = "json_object" });
                    }

                    var result = await PostJsonAsync($"{baseUrl}/chat/completions", body, settings, cancellationToken,
                        request =>
                        {
                            if (!string.IsNullOrEmpty(settings.ApiKey))
                            {
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                            }
                        });

                    if (!result.IsSuccess)
                    {
                        var unsupported = result.HttpStatus is 400 or 401 or 403 or 422
                            && (result.ErrorBody?.Contains("not supported", StringComparison.OrdinalIgnoreCase) == true
                                || result.ErrorBody?.Contains("json_object", StringComparison.OrdinalIgnoreCase) == true
                                || result.ErrorBody?.Contains("response_format", StringComparison.OrdinalIgnoreCase) == true
                                || result.ErrorBody?.Contains("reasoning", StringComparison.OrdinalIgnoreCase) == true);
                        if (attempt == 0 && unsupported)
                        {
                            continue;
                        }

                        return new LlmIntent { Error = result.Error };
                    }

                    var json = JsonNode.Parse(result.ResponseText);
                    var content = json?["choices"]?[0]?["message"]?["content"]?.ToString();
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_EMPTY_CONTENT, new { ProviderName = ProviderDisplayName }) };
                    }

                    return ParseIntentJson(content);
                }

                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_RETRY_FAILED, new { ProviderName = ProviderDisplayName }) };
            }
            catch (OperationCanceledException)
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.HTTP_REQUEST_TIMEOUT, new { ProviderName = ProviderDisplayName }) };
            }
            catch (Exception ex)
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.HTTP_EXCEPTION, new { ProviderName = ProviderDisplayName, Detail = ex.Message }) };
            }
        }
    }
}