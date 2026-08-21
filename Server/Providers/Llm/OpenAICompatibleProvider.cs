using System;
using System.Net.Http.Headers;
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
                    var useResponseFormat = attempt == 0;
                    var useReasoningEffort = attempt == 0 && !string.IsNullOrEmpty(settings.ReasoningEffort) && settings.ReasoningEffort != "default";
                    var body = BuildChatCompletionsBody(modelId, settings.SystemPrompt, userText, settings.Temperature, maxTokens);
                    if (useReasoningEffort)
                    {
                        ApplyOpenAiReasoning(body, settings.ReasoningEffort);
                    }
                    if (useResponseFormat)
                    {
                        body.ResponseFormat = new OpenAiResponseFormat { Type = "json_object" };
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
                        if (result.ErrorBody?.Contains("reasoning", StringComparison.OrdinalIgnoreCase) == true
                            || result.ErrorBody?.Contains("thinking", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            return new LlmIntent { Error = result.Error };
                        }

                        var formatUnsupported = result.HttpStatus is 400 or 401 or 403 or 422
                            && (result.ErrorBody?.Contains("not supported", StringComparison.OrdinalIgnoreCase) == true
                                || result.ErrorBody?.Contains("json_object", StringComparison.OrdinalIgnoreCase) == true
                                || result.ErrorBody?.Contains("response_format", StringComparison.OrdinalIgnoreCase) == true);

                        if (attempt == 0 && formatUnsupported)
                        {
                            continue;
                        }

                        return new LlmIntent { Error = result.Error };
                    }

                    var content = ExtractChatContentText(result.ResponseText);
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