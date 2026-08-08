
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers
{
    /// <summary>
    /// 所有云端服务商（LLM/STT）实现的公共抽象基类。
    /// 统一 HTTP 发送骨架、错误信息协议（"{Tag} ..." 前缀）、请求级超时与文本裁剪，
    /// 子类只需关注厂商协议差异（endpoint / body / 鉴权头 / 响应解析）。
    /// </summary>
    public abstract class BaseProvider
    {
        /// <summary>
        /// 错误信息厂商名前缀，例如 "Zhipu" / "讯飞"。所有错误文案均以该前缀开头。
        /// </summary>
        protected abstract string ProviderTag { get; }

        /// <summary>
        /// HTTP 发送结果。成功时 <see cref="Error"/> 为 null 且 <see cref="ResponseText"/> 为响应原文；
        /// 失败时 <see cref="HttpStatus"/>（如有）与 <see cref="ErrorBody"/>（原文）供重试/关键字判断使用。
        /// </summary>
        protected sealed class PostResponse
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
        /// 发送 JSON 请求体（application/json）。
        /// <paramref name="configureRequest"/> 用于注入鉴权/自定义头；<paramref name="truncateLen"/> 控制错误原文裁剪长度。
        /// </summary>
        protected async Task<PostResponse> SendJsonAsync(
            string endpoint,
            JObject body,
            ProviderSettings settings,
            CancellationToken cancellationToken,
            Action<HttpRequestMessage> configureRequest = null,
            int truncateLen = 320)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json"),
            };
            return await SendAsync(request, settings, cancellationToken, configureRequest, truncateLen).ConfigureAwait(false);
        }

        /// <summary>
        /// 发送原始二进制请求体（如 WAV）。
        /// </summary>
        protected async Task<PostResponse> SendRawAsync(
            string endpoint,
            byte[] content,
            string contentType,
            ProviderSettings settings,
            CancellationToken cancellationToken,
            Action<HttpRequestMessage> configureRequest = null,
            int truncateLen = 240)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new ByteArrayContent(content),
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return await SendAsync(request, settings, cancellationToken, configureRequest, truncateLen).ConfigureAwait(false);
        }

        /// <summary>
        /// 发送已构造好的请求（multipart 等特殊场景亦走此入口），统一超时、错误包装与 catch。
        /// 注意：<paramref name="request"/> 由调用方负责释放。
        /// </summary>
        protected async Task<PostResponse> SendAsync(
            HttpRequestMessage request,
            ProviderSettings settings,
            CancellationToken cancellationToken,
            Action<HttpRequestMessage> configureRequest = null,
            int truncateLen = 320)
        {
            configureRequest?.Invoke(request);
            var client = AssistantHttpClient.WithTimeout();
            using var cts = CreateTimeoutCts(settings.TimeoutSec, cancellationToken);

            try
            {
                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new PostResponse
                    {
                        HttpStatus = (int)response.StatusCode,
                        ErrorBody = responseString,
                        Error = $"{ProviderTag} HTTP {response.StatusCode}: {SafeTrim(responseString, truncateLen)}",
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
