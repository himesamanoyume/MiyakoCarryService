
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Interfaces;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Models.Providers;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;
using Newtonsoft.Json;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    public abstract class BaseLlmProvider : BaseProvider, ILlmProvider
    {
        public virtual Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            return Task.FromResult(new LlmIntent { Error = Locales.ERROR_NOT_IMPLEMENTED.McsLocalized() });
        }

        public virtual Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            return Task.FromResult(Locales.LLM_PING_NOT_SUPPORTED.McsLocalized());
        }

        protected string ExtractChatContentText(string responseString)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<OpenAiChatResponse>(responseString);
                return response?.Choices?.FirstOrDefault()?.Message?.Content;
            }
            catch
            {
                return null;
            }
        }

        protected OpenAiChatRequest BuildChatCompletionsBody(string model, string systemPrompt, string userText, double temperature, int maxTokens, string maxTokensFieldName = "max_tokens", string reasoningEffort = null)
        {
            var request = new OpenAiChatRequest
            {
                Model = model,
                Messages =
                [
                    new OpenAiChatMessage { Role = "system", Content = systemPrompt ?? "" },
                    new OpenAiChatMessage { Role = "user", Content = userText },
                ],
                Temperature = temperature,
            };
            if (maxTokensFieldName == "tokens_to_generate")
            {
                request.TokensToGenerate = maxTokens > 0 ? maxTokens : 10107;
            }
            else
            {
                request.MaxTokens = maxTokens > 0 ? maxTokens : 10107;
            }
            ApplyOpenAiReasoning(request, reasoningEffort);
            return request;
        }

        protected void ApplyOpenAiReasoning(OpenAiChatRequest request, string reasoningEffort)
        {
            if (string.IsNullOrEmpty(reasoningEffort) || reasoningEffort == "default")
            {
                return;
            }
            if (reasoningEffort == "none")
            {
                request.Thinking = new OpenAiThinking { Type = "disabled" };
                return;
            }
            request.ReasoningEffort = reasoningEffort;
        }

        public LlmIntent ParseIntentJson(string content)
        {
            try
            {
                var json = JsonConvert.DeserializeObject<LlmIntentJson>(content);
                if (json == null)
                {
                    return new LlmIntent { Error = string.Format(Locales.LLM_PARSE_ERROR.McsLocalized(), ProviderDisplayName, "null", SafeTrim(content, 240)) };
                }

                if (!string.IsNullOrWhiteSpace(json.ReplyText) || !string.IsNullOrWhiteSpace(json.Error))
                {
                    return new LlmIntent { Error = LlmIntent.NotRecognized };
                }

                var commandName = json.Command;
                if (string.IsNullOrWhiteSpace(commandName))
                {
                    return new LlmIntent { Error = string.Format(Locales.LLM_MISSING_COMMAND.McsLocalized(), ProviderDisplayName) };
                }

                var intent = new LlmIntent { CommandName = commandName };
                if (Enum.TryParse<EIntentTargetSelector>(json.Selector, ignoreCase: true, out var selector))
                {
                    intent.Selector = selector;
                }

                if (TryParseInt(json.TargetIndex, out var targetIndex))
                {
                    intent.TargetIndex = targetIndex;
                }

                if (json.TargetIndices is { Count: > 0 })
                {
                    var indices = new List<int>();
                    foreach (var item in json.TargetIndices)
                    {
                        if (TryParseInt(item, out var parsedIndex))
                        {
                            indices.Add(parsedIndex);
                        }
                    }
                    if (indices.Count > 0)
                    {
                        intent.TargetIndices = indices;
                    }
                }

                if (json.TargetCodeNames is { Count: > 0 })
                {
                    var codeNames = new List<string>();
                    foreach (var item in json.TargetCodeNames)
                    {
                        if (!string.IsNullOrWhiteSpace(item))
                        {
                            codeNames.Add(item);
                        }
                    }
                    if (codeNames.Count > 0)
                    {
                        intent.TargetCodeNames = codeNames;
                    }
                }

                if (!string.IsNullOrWhiteSpace(json.TargetCodeName))
                {
                    intent.TargetCodeName = json.TargetCodeName;
                    if (intent.Selector == EIntentTargetSelector.Unspecified && !string.IsNullOrEmpty(intent.TargetCodeName))
                    {
                        intent.Selector = EIntentTargetSelector.ByName;
                    }
                }

                if (intent.Selector == EIntentTargetSelector.Unspecified)
                {
                    intent.Selector = intent.TargetIndices != null || intent.TargetIndex.HasValue
                        ? EIntentTargetSelector.ByIndex
                        : intent.TargetCodeNames != null || !string.IsNullOrEmpty(intent.TargetCodeName)
                            ? EIntentTargetSelector.ByName
                            : EIntentTargetSelector.All;
                }

                if (!string.IsNullOrWhiteSpace(json.AimingBodyPart))
                {
                    intent.AimingBodyPart = json.AimingBodyPart;
                }

                if (TryParseInt(json.OptionIndex, out var optionIndex))
                {
                    intent.OptionIndex = optionIndex;
                }

                return intent;
            }
            catch (Exception ex)
            {
                return new LlmIntent { Error = string.Format(Locales.LLM_PARSE_ERROR.McsLocalized(), ProviderDisplayName, ex.Message, SafeTrim(content, 240)) };
            }
        }

        private static bool TryParseInt(string s, out int value)
        {
            return int.TryParse(s, out value);
        }

        public virtual Task<PostResponse> PostAsync(string baseUrl, object body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
