using System;
using System.Net;
using System.Net.Http;

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

        public static HttpClient Shared => _shared ?? Init();

        public static HttpClient Init()
        {
            if (_shared != null)
            {
                return null;
            }

            _shared = CreateClient(false);
            _shared.DefaultRequestHeaders.ConnectionClose = false;
            _shared.DefaultRequestHeaders.ExpectContinue = false;
            return _shared;
        }

        private static HttpClient CreateClient(bool useProxy)
        {
            var handler = new HttpClientHandler();
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

        public static HttpClient WithTimeout()
        {
            ApplyProxy();
            return _shared;
        }
    }
}