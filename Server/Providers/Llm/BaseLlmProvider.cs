using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Interfaces;
using MiyakoCarryService.Server.Models.Llm;
using ProviderModels = MiyakoCarryService.Server.Models.Providers;
using MiyakoCarryService.Server.Utils;
using SPTarkov.Server.Core.Services.Locales;

namespace MiyakoCarryService.Server.Providers.Llm
{
    public abstract class BaseLlmProvider : BaseProvider, ILlmProvider
    {
        protected BaseLlmProvider(ServerLocalisationService serverLocalisation) : base(serverLocalisation)
        {
        }

        public virtual async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.ERROR_NOT_IMPLEMENTED) };
        }

        protected string ExtractChatContentText(string responseString)
        {
            try
            {
                var response = JsonSerializer.Deserialize<ProviderModels.OpenAiChatResponse>(responseString);
                return response?.Choices?.FirstOrDefault()?.Message?.Content;
            }
            catch
            {
                return null;
            }
        }

        protected ProviderModels.OpenAiChatRequest BuildChatCompletionsBody(string model, string systemPrompt, string userText, double temperature, int maxTokens, string maxTokensFieldName = "max_tokens")
        {
            var request = new ProviderModels.OpenAiChatRequest
            {
                Model = model,
                Messages =
                [
                    new ProviderModels.OpenAiChatMessage { Role = "system", Content = systemPrompt ?? "" },
                    new ProviderModels.OpenAiChatMessage { Role = "user", Content = userText },
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
            return request;
        }

        public virtual string ExtractText(string responseString)
        {
            throw new NotImplementedException();
        }

        public LlmIntent ParseIntentJson(string content)
        {
            try
            {
                var json = JsonSerializer.Deserialize<ProviderModels.McsChatIntent>(content);
                if (json == null)
                {
                    return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_MISSING_FIELD, new { ProviderName = ProviderDisplayName }) };
                }

                if (!string.IsNullOrEmpty(json.ReplyText))
                {
                    return new LlmIntent { ReplyText = json.ReplyText };
                }

                if (json.Order != null)
                {
                    return new LlmIntent
                    {
                        Order = new OrderIntent
                        {
                            Players = json.Order.Players ?? 0,
                            SpawnTypeIndex = json.Order.SpawnTypeIndex ?? 0,
                            Level = json.Order.Level ?? 0,
                            Duration = json.Order.Duration ?? 0,
                        },
                    };
                }

                if (json.Ticket != null)
                {
                    return new LlmIntent
                    {
                        Ticket = new TicketIntent { Percent = json.Ticket.Percent ?? 0 },
                    };
                }

                if (json.Renew != null)
                {
                    return new LlmIntent
                    {
                        Renew = new RenewIntent { Target = json.Renew.Target ?? string.Empty },
                    };
                }

                if (json.Settle != null)
                {
                    return new LlmIntent
                    {
                        Settle = new SettleIntent { Target = json.Settle.Target ?? string.Empty },
                    };
                }

                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_MISSING_FIELD, new { ProviderName = ProviderDisplayName }) };
            }
            catch (Exception ex)
            {
                return new LlmIntent { Error = _serverLocalisationService.GetText(Locales.LLM_PARSE_ERROR, new { ProviderName = ProviderDisplayName, Detail = ex.Message, Raw = SafeTrim(content, 240) }) };
            }
        }
    }
}
