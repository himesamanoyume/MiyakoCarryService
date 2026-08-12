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
    public sealed class BaiduAsrProvider : BaseSttProvider
    {
        protected override string ProviderDisplayName => Locales.STTPROVIDERBAIDUASR.McsLocalized();

        private const string DefaultBaseUrl = "https://vop.baidubce.com";

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings?.ApiKey) || string.IsNullOrEmpty(settings.ApiSecret))
            {
                return new SttResult { Error = Locales.STT_BAIDU_REQUIRED.McsLocalized() };
            }
            if (!TryPrepare16kWav(audio, out var wavBytes, out var prepareError))
            {
                return new SttResult { Error = prepareError };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var client = AssistantHttpClient.WithTimeout();

            // 1. 换取 access_token
            using (var tokenCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                tokenCts.CancelAfter(TimeSpan.FromSeconds(15));
                var token = await FetchAccessTokenAsync(client, settings, tokenCts.Token).ConfigureAwait(false);
                if (token == null)
                {
                    return new SttResult { Error = Locales.STT_BAIDU_TOKEN.McsLocalized() };
                }

                // 2. 一句话识别
                var body = new JObject
                {
                    ["format"] = "wav",
                    ["rate"] = RequiredRate,
                    ["channel"] = 1,
                    ["cuid"] = "miyako-carry-service",
                    ["token"] = token,
                    ["speech"] = Convert.ToBase64String(wavBytes),
                };

                var result = await SendJsonAsync($"{baseUrl}/server_api", body, settings, cancellationToken,
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
                var errNo = json.Value<int>("err_no");
                if (errNo != 0)
                {
                    return new SttResult { Error = $"百度识别失败 {errNo}: {json.Value<string>("err_msg") ?? Locales.UNKNOWN_ERROR.McsLocalized()}" };
                }
                var text = json["result"]?[0]?.ToString() ?? string.Empty;
                return new SttResult { Text = text, DetectedLanguage = settings.Language };
            }
        }

        private static async Task<string> FetchAccessTokenAsync(HttpClient client, ProviderSettings settings, CancellationToken ct)
        {
            try
            {
                var url = "https://aip.baidubce.com/oauth/2.0/token" +
                    $"?grant_type=client_credentials&client_id={Uri.EscapeDataString(settings.ApiKey)}&client_secret={Uri.EscapeDataString(settings.ApiSecret)}";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                var json = JObject.Parse(responseString);
                var token = json.Value<string>("access_token");
                return string.IsNullOrEmpty(token) ? null : token;
            }
            catch
            {
                return null;
            }
        }
    }
}
