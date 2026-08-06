using System;
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

        public static HttpClient Shared => _shared ?? throw new InvalidOperationException("AssistantHttpClient 未初始化");

        public static void Initialize()
        {
            if (_shared != null)
            {
                return;
            }

            var handler = new HttpClientHandler();
            try
            {
                handler.MaxConnectionsPerServer = 8;
            }
            catch (NotImplementedException)
            {
                // EFT 基于 Mono 运行时（MonoWebRequestHandler），未实现 MaxConnectionsPerServer，忽略该设置。
            }

            _shared = new HttpClient(handler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(60),
            };
            _shared.DefaultRequestHeaders.ConnectionClose = false;
            _shared.DefaultRequestHeaders.ExpectContinue = false;
        }

        /// <summary>
        /// 返回共享 HttpClient。不再改写其 Timeout（固定 60s，与商人侧实现一致）；
        /// 各服务商在请求内自行用 CancellationTokenSource + CancelAfter 控制超时，避免并发互相干扰。
        /// </summary>
        public static HttpClient WithTimeout(ProviderSettings settings)
        {
            return _shared;
        }

        public static HttpClient WithTimeout(int timeoutSec)
        {
            return _shared;
        }
    }
}