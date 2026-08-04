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

        public static HttpClient WithTimeout(ProviderSettings settings)
        {
            var timeout = settings?.TimeoutSec > 0
                ? TimeSpan.FromSeconds(settings.TimeoutSec)
                : TimeSpan.FromSeconds(30);
            if (_shared.Timeout != timeout)
            {
                _shared.Timeout = timeout;
            }
            return _shared;
        }

        public static HttpClient WithTimeout(int timeoutSec)
        {
            return WithTimeout(new ProviderSettings { TimeoutSec = timeoutSec });
        }
    }
}