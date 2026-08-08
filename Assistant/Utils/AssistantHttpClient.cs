using System;
using System.Net;
using System.Net.Http;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Utils
{
    /// <summary>
    /// 复用的 HttpClient 单源。所有 STT/LLM 服务商实现共用该实例，便于统一超时与限流设置。
    /// </summary>
    internal static class AssistantHttpClient
    {
        private static HttpClient _shared;
        private static string _appliedProxyHost;
        private static string _appliedProxyPort;

        public static HttpClient Shared => _shared ?? throw new InvalidOperationException("AssistantHttpClient 未初始化");

        public static void Init()
        {
            if (_shared != null)
            {
                return;
            }

            _shared = CreateClient(false);
            _shared.DefaultRequestHeaders.ConnectionClose = false;
            _shared.DefaultRequestHeaders.ExpectContinue = false;
        }

        private static HttpClient CreateClient(bool useProxy)
        {
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = 8
            };

            if (useProxy)
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(_appliedProxyHost, int.Parse(_appliedProxyPort));
            }
            else
            {
                handler.UseProxy = false;
            }

            return new HttpClient(handler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(60),
            };
        }

        /// <summary>
        /// 按 HttpProxyHost/HttpProxyPort 配置应用代理（host 与 port 均非空且端口可解析时全量经代理转发，
        /// 含本地地址；否则直连）。仅在配置变化时重建共享 HttpClient。
        /// </summary>
        public static void ApplyProxy()
        {
            var host = MiyakoCarryServiceAssistantPlugin.HttpProxyHost.Value;
            var port = MiyakoCarryServiceAssistantPlugin.HttpProxyPort.Value;
            var portValid = int.TryParse(port, out var parsedPort) && parsedPort > 0;

            var useProxy = !string.IsNullOrEmpty(host) && portValid;
            var effectiveHost = useProxy ? host : string.Empty;
            var effectivePort = useProxy ? parsedPort.ToString() : string.Empty;

            if (_shared == null)
            {
                return;
            }

            if (_appliedProxyHost == effectiveHost && _appliedProxyPort == effectivePort)
            {
                return;
            }

            _appliedProxyHost = effectiveHost;
            _appliedProxyPort = effectivePort;

            var old = _shared;
            _shared = CreateClient(useProxy);
            _shared.DefaultRequestHeaders.ConnectionClose = false;
            _shared.DefaultRequestHeaders.ExpectContinue = false;
            try
            {
                old.Dispose();
            }
            catch
            {

            }
        }

        /// <summary>
        /// 返回共享 HttpClient（确保代理配置已应用）。超时不在共享实例上改写（固定 60s），
        /// 各服务商在请求内自行以 CancellationTokenSource + CancelAfter 控制超时，避免并发互相干扰。
        /// </summary>
        public static HttpClient WithTimeout()
        {
            ApplyProxy();
            return _shared;
        }
    }
}