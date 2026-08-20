using System;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Models.Providers;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    public sealed class XfyunIatProvider : BaseSttProvider
    {
        protected override string ProviderDisplayName => Locales.STTPROVIDERXFYUNIAT.McsLocalized();

        private const string DefaultBaseUrl = "https://iat-api.xfyun.cn";

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings?.ApiKey) || string.IsNullOrEmpty(settings.ApiSecret))
            {
                return new SttResult { Error = Locales.STT_XFYUN_REQUIRED.McsLocalized() };
            }
            if (string.IsNullOrEmpty(settings.ModelId))
            {
                return new SttResult { Error = Locales.STT_XFYUN_APPID.McsLocalized() };
            }
            if (!TryPrepare16kWav(audio, out var wavBytes, out var prepareError))
            {
                return new SttResult { Error = prepareError };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var host = new Uri(baseUrl).Host;
            var date = DateTime.UtcNow.ToString("R");
            var authorization = BuildAuthorization(host, date, settings.ApiKey, settings.ApiSecret);

            var body = new XfyunIatRequest
            {
                Common = new XfyunIatCommon { AppId = settings.ModelId },
                Business = new XfyunIatBusiness
                {
                    Aue = "raw",
                    Auf = $"audio/L16;rate={RequiredRate}",
                    VadEos = 3000,
                    Domain = "iat",
                    Language = string.IsNullOrEmpty(settings.Language) ? "zh_cn" : settings.Language,
                },
                Data = new XfyunIatData
                {
                    Audio = Convert.ToBase64String(wavBytes),
                    SampleRate = RequiredRate,
                },
            };

            var result = await SendJsonAsync($"{baseUrl}/v2/iat", body, settings, cancellationToken,
                request =>
                {
                    // 讯飞要求 content-type 不带 charset 参数
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    request.Headers.Add("Authorization", authorization);
                    request.Headers.Add("Date", date);
                },
                truncateLen: 240
            );

            if (!result.IsSuccess)
            {
                return new SttResult { Error = result.Error };
            }

            var response = ParseResponseJson<XfyunIatResponse>(result);
            if (response == null)
            {
                return new SttResult { Error = string.Format(Locales.STT_RESPONSE_PARSE_FAILED.McsLocalized(), ProviderDisplayName) };
            }
            var code = response.Code ?? 0;
            if (code != 0)
            {
                return new SttResult { Error = $"Error {code}: {response.Message ?? Locales.UNKNOWN_ERROR.McsLocalized()}" };
            }

            var sb = new StringBuilder();
            if (response.Data?.Result?.Rg is { Count: > 0 })
            {
                foreach (var item in response.Data.Result.Rg)
                {
                    var v = item?.V;
                    if (string.IsNullOrEmpty(v))
                    {
                        continue;
                    }
                    try
                    {
                        sb.Append(Encoding.UTF8.GetString(Convert.FromBase64String(v)));
                    }
                    catch
                    {
                        
                    }
                }
            }
            return new SttResult { Text = sb.ToString(), DetectedLanguage = settings.Language };
        }

        private string BuildAuthorization(string host, string date, string apiKey, string apiSecret)
        {
            var signatureOrigin = $"host: {host}\ndate: {date}\nPOST /v2/iat HTTP/1.1";
            string signature;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret)))
            {
                signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureOrigin)));
            }
            return $"api_key=\"{apiKey}\", algorithm=\"hmac-sha256\", headers=\"host date request-line\", signature=\"{signature}\"";
        }
    }
}
