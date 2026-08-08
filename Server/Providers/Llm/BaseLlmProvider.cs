using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Interfaces;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Services.Providers.Llm
{
    /// <summary>
    /// 服务端 LLM 服务商适配器基类。所有厂商共享同一份 <see cref="LlmProviderSettings"/> 配置项，
    /// 并共用同一个共享 HttpClient（含代理应用）。
    /// </summary>
    public abstract class BaseLlmProvider : ILlmProvider
    {
        /// <summary>错误信息厂商名前缀（如 "OpenAI-Compat"），子类可覆盖。</summary>
        protected string ProviderTag
        {
            get
            {
                return field ??= GetType().Name;
            }
        }

        private static HttpClient _sharedClient = CreateClient(useProxy: false);
        private static string _appliedProxyHost;
        private static string _appliedProxyPort;

        /// <summary>所有子类共享的 HttpClient 单源，避免重复创建连接池。</summary>
        protected static HttpClient SharedClient
        {
            get { return _sharedClient; }
        }

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

            var old = _sharedClient;
            _sharedClient = CreateClient(useProxy);
            try
            {
                old.Dispose();
            }
            catch
            {
                
            }
        }

        private static HttpClient CreateClient(bool useProxy)
        {
            if (useProxy)
            {
                var handler = new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = new System.Net.WebProxy(_appliedProxyHost, int.Parse(_appliedProxyPort)),
                };
                return new HttpClient(handler, disposeHandler: true)
                {
                    Timeout = TimeSpan.FromSeconds(60),
                };
            }
            return new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(60),
            };
        }

        /// <summary>
        /// HTTP 发送结果。成功时 <see cref="Error"/> 为 null 且 <see cref="ResponseText"/> 为响应原文；
        /// 失败时 <see cref="HttpStatus"/>（如有）与 <see cref="ErrorBody"/>（原文）供重试/关键字判断使用。
        /// </summary>
        public sealed class PostResponse
        {
            public string ResponseText;
            public int? HttpStatus;
            public string ErrorBody;
            public string Error;

            public bool IsSuccess
            {
                get { return Error == null; }
            }
        }

        public virtual async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "此接口未实现" };
        }

        /// <summary>
        /// 提取 OpenAI 兼容响应中 <c>choices[0].message.content</c> 的文本；
        /// 解析失败或内容为空时返回 null。
        /// </summary>
        protected static string ExtractChatContentText(string responseString)
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
        protected static JsonObject BuildChatCompletionsBody(string model, string systemPrompt, string userText, double temperature, int maxTokens, string maxTokensFieldName = "max_tokens")
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
        /// 统一 JSON POST 骨架：请求级超时、错误包装（"{ProviderTag} ..." 前缀）与 catch 均在此处理，
        /// 子类只需提供 endpoint/body，并通过 <paramref name="configureRequest"/> 注入鉴权/自定义头。
        /// 注意：<paramref name="configureRequest"/> 在 try 块内执行，其异常统一按 "{ProviderTag} 异常" 处理。
        /// </summary>
        protected async Task<PostResponse> PostJsonAsync(
            string endpoint,
            JsonObject body,
            LlmProviderSettings settings,
            CancellationToken cancellationToken,
            Action<HttpRequestMessage> configureRequest = null)
        {
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
                };
                configureRequest?.Invoke(request);

                using var response = await SharedClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new PostResponse
                    {
                        HttpStatus = (int)response.StatusCode,
                        ErrorBody = responseString,
                        Error = $"{ProviderTag} HTTP {response.StatusCode}: {SafeTrim(responseString, 320)}",
                    };
                }
                return new PostResponse { ResponseText = responseString };
            }
            catch (OperationCanceledException)
            {
                return new PostResponse { Error = $"{ProviderTag} 请求超时" };
            }
            catch (Exception ex)
            {
                return new PostResponse { Error = $"{ProviderTag} 异常：{ex.Message}" };
            }
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

        public virtual string ExtractText(string responseString)
        {
            throw new NotImplementedException();
        }
    }
}
