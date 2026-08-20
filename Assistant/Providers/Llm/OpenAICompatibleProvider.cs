using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Models.Providers;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    public sealed class OpenAICompatibleProvider : BaseLlmProvider
    {
        private const string DefaultBaseUrl = "https://api.deepseek.com";
        private const string DefaultModel = "deepseek-v4-flash";

        protected override string ProviderDisplayName => Locales.LLMPROVIDEROPENAICOMPATIBLE.McsLocalized();

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = Locales.LLM_USER_TEXT_EMPTY.McsLocalized() };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);

            try
            {
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    var useResponseFormat = attempt == 0;
                    var useReasoningEffort = attempt == 0 && !string.IsNullOrEmpty(settings.ReasoningEffort) && settings.ReasoningEffort != "default";
                    var body = BuildChatCompletionsBody(model, systemPrompt, userText, settings.Temperature, settings.MaxTokens);
                    if (useReasoningEffort)
                    {
                        body.ReasoningEffort = settings.ReasoningEffort;
                    }
                    if (useResponseFormat)
                    {
                        body.ResponseFormat = new OpenAiResponseFormat { Type = "json_object" };
                    }

                    var result = await SendJsonAsync($"{baseUrl}/chat/completions", body, settings, cancellationToken,
                        request =>
                        {
                            if (!string.IsNullOrEmpty(settings.ApiKey))
                            {
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                            }
                        });

                    if (!result.IsSuccess)
                    {
                        if (result.ErrorBody?.Contains("reasoning", StringComparison.OrdinalIgnoreCase) == true)
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
                        return new LlmIntent { Error = string.Format(Locales.LLM_EMPTY_CONTENT.McsLocalized(), ProviderDisplayName) };
                    }

                    return ParseIntentJson(content);
                }

                return new LlmIntent { Error = string.Format(Locales.LLM_RETRY_FAILED.McsLocalized(), ProviderDisplayName) };
            }
            catch (OperationCanceledException)
            {
                return new LlmIntent { Error = string.Format(Locales.HTTP_REQUEST_TIMEOUT.McsLocalized(), ProviderDisplayName) };
            }
            catch (Exception ex)
            {
                return new LlmIntent { Error = string.Format(Locales.HTTP_EXCEPTION.McsLocalized(), ProviderDisplayName, ex.Message) };
            }
        }

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;

            var body = BuildChatCompletionsBody(model, "You are a connectivity test. Reply with exactly: pong", "ping", 0d, 64);

            var result = await SendJsonAsync($"{baseUrl}/chat/completions", body, settings, cancellationToken,
                request =>
                {
                    if (!string.IsNullOrEmpty(settings.ApiKey))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                    }
                });
            if (!result.IsSuccess)
            {
                return result.Error;
            }

            var content = ExtractChatContentText(result.ResponseText);
            return string.IsNullOrWhiteSpace(content) ? Locales.LLM_EMPTY_RESPONSE.McsLocalized() : content;
        }
    }
}
