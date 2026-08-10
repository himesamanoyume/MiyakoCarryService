
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers
{
    public abstract class BaseProvider
    {
        protected string ProviderTag
        {
            get
            {
                return field ??= GetType().Name;
            }
        }

        protected virtual string ProviderDisplayName => ProviderTag;

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
                return new PostResponse { Error = string.Format(Locales.HTTP_REQUEST_TIMEOUT.McsLocalized(), ProviderDisplayName) };
            }
            catch (Exception ex)
            {
                return new PostResponse { Error = string.Format(Locales.HTTP_EXCEPTION.McsLocalized(), ProviderDisplayName, ex.Message) };
            }
        }
    }
}
