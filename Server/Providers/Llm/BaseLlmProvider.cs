using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Interfaces;
using MiyakoCarryService.Server.Models.Llm;
using MiyakoCarryService.Server.Providers;

namespace MiyakoCarryService.Server.Providers.Llm
{
    /// <summary>
    /// 服务端 LLM 服务商适配器基类。所有厂商共享同一份 <see cref="LlmProviderSettings"/> 配置项。
    /// </summary>
    public abstract class BaseLlmProvider : BaseProvider, ILlmProvider
    {
        public virtual async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "此接口未实现" };
        }

        /// <summary>
        /// 提取 OpenAI 兼容响应中 <c>choices[0].message.content</c> 的文本；
        /// 解析失败或内容为空时返回 null。
        /// </summary>
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

        /// <summary>
        /// 构造 OpenAI 兼容 Chat Completions 请求体（model/messages/temperature/max_tokens）。
        /// <paramref name="maxTokensFieldName"/> 可定制输出 token 上限字段名（如 MiniMax 的 tokens_to_generate）。
        /// </summary>
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
                [maxTokensFieldName] = maxTokens > 0 ? maxTokens : 3000,
            };
        }

        /// <summary>
        /// 提取厂商响应中的模型文本（各厂商响应结构不同，由子类 override）。
        /// 解析失败或内容为空时返回 null。
        /// </summary>
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

                return new LlmIntent { Error = "OpenAI-Compat 响应缺少 order/ticket/renew/settle/replyText 字段" };
            }
            catch (Exception ex)
            {
                return new LlmIntent { Error = $"OpenAI-Compat 解析失败：{ex.Message}；原文：{SafeTrim(content, 240)}" };
            }
        }
    }
}
