using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Interfaces;
using MiyakoCarryService.Server.Models.Llm;
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
                var node = JsonNode.Parse(responseString);
                return node?["choices"]?[0]?["message"]?["content"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        protected JsonObject BuildChatCompletionsBody(string model, string systemPrompt, string userText, double temperature, int maxTokens, string maxTokensFieldName = "max_tokens")
        {
            return new JsonObject
            {
                ["model"] = model,
                ["messages"] = JsonSerializer.SerializeToNode(new[]
                {
                    new { role = "system", content = systemPrompt ?? "" },
                    new { role = "user", content = userText },
                }),
                ["temperature"] = temperature,
                [maxTokensFieldName] = maxTokens > 0 ? maxTokens : 10107,
            };
        }

        public virtual string ExtractText(string responseString)
        {
            throw new NotImplementedException();
        }

        public LlmIntent ParseIntentJson(string content)
        {
            try
            {
                var node = JsonNode.Parse(content);
                if (node?["replyText"] is JsonNode reply && reply.GetValueKind() != JsonValueKind.Null)
                {
                    var replyText = reply.ToString();
                    if (!string.IsNullOrWhiteSpace(replyText))
                    {
                        return new LlmIntent { ReplyText = replyText };
                    }
                }

                if (node?["order"] is JsonNode orderNode)
                {
                    var players = orderNode["players"]?.GetValue<int>() ?? 0;
                    var spawnTypeIndex = orderNode["spawnTypeIndex"]?.GetValue<int>() ?? 0;
                    var level = orderNode["level"]?.GetValue<int>() ?? 0;
                    var duration = orderNode["duration"]?.GetValue<int>() ?? 0;

                    return new LlmIntent
                    {
                        Order = new OrderIntent
                        {
                            Players = players,
                            SpawnTypeIndex = spawnTypeIndex,
                            Level = level,
                            Duration = duration,
                        },
                    };
                }

                if (node?["ticket"] is JsonNode ticketNode)
                {
                    var percent = ticketNode["percent"]?.GetValue<int>() ?? 0;
                    return new LlmIntent
                    {
                        Ticket = new TicketIntent { Percent = percent },
                    };
                }

                if (node?["renew"] is JsonNode renewNode)
                {
                    return new LlmIntent
                    {
                        Renew = new RenewIntent { Target = renewNode["target"]?.ToString() ?? string.Empty },
                    };
                }

                if (node?["settle"] is JsonNode settleNode)
                {
                    return new LlmIntent
                    {
                        Settle = new SettleIntent { Target = settleNode["target"]?.ToString() ?? string.Empty },
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
