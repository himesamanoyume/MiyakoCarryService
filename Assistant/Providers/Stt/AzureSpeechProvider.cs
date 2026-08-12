using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    public sealed class AzureSpeechProvider : BaseSttProvider
    {
        protected override string ProviderDisplayName => Locales.STTPROVIDERAZURESPEECH.McsLocalized();

        private const string DefaultBaseUrl = "https://eastasia.stt.speech.microsoft.com";

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new SttResult { Error = string.Format(Locales.STT_APIKEY_MISSING.McsLocalized(), "Azure Subscription Key") };
            }
            if (!TryPrepareWav(audio, out var wavBytes, out var prepareError))
            {
                return new SttResult { Error = prepareError };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var endpoint = $"{baseUrl}/speech/recognition/conversation/cognitiveservices/v1" +
                $"?language={Uri.EscapeDataString(string.IsNullOrEmpty(settings.Language) ? "zh-CN" : settings.Language)}&format=simple";

            var result = await SendRawAsync(endpoint, wavBytes, "audio/wav", settings, cancellationToken,
                request =>
                {
                    request.Content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("codecs", "audio/pcm"));
                    request.Headers.Add("Ocp-Apim-Subscription-Key", settings.ApiKey);
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
            var status = json.Value<string>("RecognitionStatus");
            if (string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase))
            {
                return new SttResult
                {
                    Text = json.Value<string>("DisplayText") ?? string.Empty,
                    DetectedLanguage = settings.Language,
                };
            }

            return new SttResult { Error = string.Format(Locales.STT_STATUS_ABNORMAL.McsLocalized(), ProviderDisplayName, status ?? Locales.UNKNOWN.McsLocalized()) };
        }
    }
}
