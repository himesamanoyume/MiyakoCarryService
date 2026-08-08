using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Interfaces;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Services.Llm.Providers
{
    /// <summary>
    /// 服务端 LLM 服务商适配器接口。所有厂商共享同一份 <see cref="LlmProviderSettings"/> 配置项。
    /// </summary>
    public abstract class BaseLlmProvider : ILlmProvider
    {
        protected HttpClient SharedClient = new()
        {
            Timeout = TimeSpan.FromSeconds(60),
        };

        private string _appliedProxyHost;
        private string _appliedProxyPort;

        /// <summary>
        /// 按 HttpProxyHost/HttpProxyPort 应用代理（host 与 port 均非空且端口可解析时全量经代理转发，
        /// 含本地地址；否则直连）。仅在配置变化时重建共享 HttpClient。
        /// </summary>
        public void ApplyProxy(string host, string port)
        {
            var portValid = int.TryParse(port, out var parsedPort) && parsedPort > 0;
            var useProxy = !string.IsNullOrEmpty(host) && portValid;
            var effectiveHost = useProxy ? host : string.Empty;
            var effectivePort = useProxy ? parsedPort.ToString() : string.Empty;

            if (_appliedProxyHost == effectiveHost && _appliedProxyPort == effectivePort)
            {
                return;
            }

            _appliedProxyHost = effectiveHost;
            _appliedProxyPort = effectivePort;

            var old = SharedClient;
            if (useProxy)
            {
                var handler = new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = new System.Net.WebProxy(effectiveHost, parsedPort),
                };
                SharedClient = new HttpClient(handler, disposeHandler: true)
                {
                    Timeout = TimeSpan.FromSeconds(60),
                };
            }
            else
            {
                SharedClient = new HttpClient()
                {
                    Timeout = TimeSpan.FromSeconds(60),
                };
            }
            try { old.Dispose(); } catch { }
        }
        
        public virtual async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "此接口未实现" };
        }

        public string SafeTrim(string s, int max)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            return s.Length <= max ? s : s.Substring(0, max) + "...";
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