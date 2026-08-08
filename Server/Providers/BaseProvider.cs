using System;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Providers
{
    /// <summary>
    /// 服务端所有云端服务商（LLM）实现的公共抽象基类。
    /// 统一 HTTP 发送骨架、错误信息协议（"{Tag} ..." 前缀）、请求级超时与文本裁剪，
    /// 子类只需关注厂商协议差异（endpoint / body / 鉴权头 / 响应解析）。
    /// </summary>
    public abstract class BaseProvider
    {
        /// <summary>
        /// 错误信息厂商名前缀，默认取类名（如 "AnthropicProvider"），子类可覆盖。
        /// 所有错误文案均以该前缀开头。
        /// </summary>
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

        public string SafeTrim(string s, int max)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        /// <summary>
        /// 请求级超时：按配置精确生效，不与其它请求互相干扰；超时未配置或非法时默认 30s。
        /// </summary>
        protected static CancellationTokenSource CreateTimeoutCts(int timeoutSec, CancellationToken cancellationToken)
        {
            var timeout = timeoutSec > 0 ? TimeSpan.FromSeconds(timeoutSec) : TimeSpan.FromSeconds(30);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            return cts;
        }

        /// <summary>
        /// 统一 JSON POST 骨架：请求级超时、错误包装（"{ProviderTag} ..." 前缀）与 catch 均在此处理，
        /// 子类只需提供 endpoint/body，并通过 <paramref name="configureRequest"/> 注入鉴权/自定义头。
        /// </summary>
        protected async Task<PostResponse> PostJsonAsync(
            string endpoint,
            JsonObject body,
            LlmProviderSettings settings,
            CancellationToken cancellationToken,
            Action<HttpRequestMessage> configureRequest = null)
        {
            using var cts = CreateTimeoutCts(settings.TimeoutSec, cancellationToken);

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
    }
}
