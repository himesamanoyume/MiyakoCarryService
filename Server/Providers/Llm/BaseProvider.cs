using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;
using MiyakoCarryService.Server.Utils;
using SPTarkov.Server.Core.Services.Locales;

namespace MiyakoCarryService.Server.Providers.Llm
{
    public abstract class BaseProvider
    {
        protected readonly ServerLocalisationService _serverLocalisationService;

        protected BaseProvider(ServerLocalisationService serverLocalisationService)
        {
            _serverLocalisationService = serverLocalisationService;
        }

        protected string ProviderTag => field ??= GetType().Name;

        protected virtual string ProviderDisplayName => ProviderTag;

        private static HttpClient _sharedClient = CreateClient(useProxy: false);
        private static string _appliedProxyHost;
        private static string _appliedProxyPort;

        protected static HttpClient SharedClient => _sharedClient;

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

        protected static CancellationTokenSource CreateTimeoutCts(int timeoutSec, CancellationToken cancellationToken)
        {
            var timeout = timeoutSec > 0 ? TimeSpan.FromSeconds(timeoutSec) : TimeSpan.FromSeconds(30);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            return cts;
        }

        /// <summary>
        /// DTO 序列化设置：忽略未赋值的可空字段。
        /// </summary>
        protected static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        };

        protected async Task<PostResponse> PostJsonAsync<TBody>(
            string endpoint,
            TBody body,
            LlmProviderSettings settings,
            CancellationToken cancellationToken,
            Action<HttpRequestMessage> configureRequest = null)
        {
            using var cts = CreateTimeoutCts(settings.TimeoutSec, cancellationToken);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json"),
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
                        Error = _serverLocalisationService.GetText(Locales.HTTP_STATUS_ERROR, new
                        {
                            ProviderName = ProviderDisplayName,
                            StatusCode = (int)response.StatusCode,
                            Detail = SafeTrim(responseString, 320),
                        }),
                    };
                }
                return new PostResponse { ResponseText = responseString };
            }
            catch (OperationCanceledException)
            {
                return new PostResponse { Error = _serverLocalisationService.GetText(Locales.HTTP_REQUEST_TIMEOUT, new { ProviderName = ProviderDisplayName }) };
            }
            catch (Exception ex)
            {
                return new PostResponse { Error = _serverLocalisationService.GetText(Locales.HTTP_EXCEPTION, new { ProviderName = ProviderDisplayName, Detail = ex.Message }) };
            }
        }
    }
}