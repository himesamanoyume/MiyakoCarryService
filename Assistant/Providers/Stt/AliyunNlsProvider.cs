using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    public sealed class AliyunNlsProvider : BaseSttProvider
    {
        protected override string ProviderDisplayName => Locales.STTPROVIDERALIYUNNLS.McsLocalized();

        private const string DefaultGateway = "https://nls-gateway-cn-shanghai.aliyuncs.com";
        private const string DefaultTokenApi = "https://nls-meta.cn-shanghai.aliyuncs.com";

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings?.ApiKey) || string.IsNullOrEmpty(settings.ApiSecret))
            {
                return new SttResult { Error = Locales.STT_ALIYUN_REQUIRED.McsLocalized() };
            }
            if (string.IsNullOrEmpty(settings.ModelId))
            {
                return new SttResult { Error = Locales.STT_ALIYUN_APPID.McsLocalized() };
            }
            if (!TryPrepare16kWav(audio, out var wavBytes, out var prepareError))
            {
                return new SttResult { Error = prepareError };
            }

            var gateway = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultGateway : settings.BaseUrl.TrimEnd('/');
            var client = AssistantHttpClient.WithTimeout();

            using (var tokenCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                tokenCts.CancelAfter(TimeSpan.FromSeconds(15));
                var token = await FetchTokenAsync(client, settings, tokenCts.Token).ConfigureAwait(false);
                if (string.IsNullOrEmpty(token))
                {
                    return new SttResult { Error = Locales.STT_ALIYUN_TOKEN.McsLocalized() };
                }

                var endpoint = $"{gateway}/stream/v1/asr?appkey={Uri.EscapeDataString(settings.ModelId)}" +
                    $"&format=wav&sample_rate={RequiredRate}&enable_punctuation_prediction=true&enable_inverse_text_normalization=true";
                var result = await SendRawAsync(endpoint, wavBytes, "application/octet-stream", settings, cancellationToken,
                    request =>
                    {
                        request.Headers.Add("X-NLS-AppKey", settings.ModelId);
                        request.Headers.Add("X-NLS-Token", token);
                    },
                    truncateLen: 240);
                if (!result.IsSuccess)
                {
                    return new SttResult { Error = result.Error };
                }

                var json = ParseResponseJson(result);
                if (json == null)
                {
                    return new SttResult { Error = string.Format(Locales.STT_RESPONSE_PARSE_FAILED.McsLocalized(), ProviderDisplayName) };
                }
                var status = json.Value<int>("status");
                if (status != 20000000)
                {
                    return new SttResult { Error = $"Error {status}: {json.Value<string>("message") ?? Locales.UNKNOWN_ERROR.McsLocalized()}" };
                }
                return new SttResult { Text = json.Value<string>("result") ?? string.Empty, DetectedLanguage = settings.Language };
            }
        }

        private async Task<string> FetchTokenAsync(HttpClient client, ProviderSettings settings, CancellationToken ct)
        {
            try
            {
                var url = $"{DefaultTokenApi}/api/v1/token?AccessKeyId={Uri.EscapeDataString(settings.ApiKey)}&AccessKeySecret={Uri.EscapeDataString(settings.ApiSecret)}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                var json = JObject.Parse(responseString);
                return json["Token"]?["Id"]?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
