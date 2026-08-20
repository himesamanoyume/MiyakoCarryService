using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Models.Providers;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    public sealed class GoogleSpeechProvider : BaseSttProvider
    {
        protected override string ProviderDisplayName => Locales.STTPROVIDERGOOGLESPEECH.McsLocalized();

        private const string DefaultBaseUrl = "https://speech.googleapis.com";

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new SttResult { Error = string.Format(Locales.STT_APIKEY_MISSING.McsLocalized(), "Google API Key") };
            }
            if (!TryPrepareWav(audio, out var wavBytes, out var prepareError))
            {
                return new SttResult { Error = prepareError };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var endpoint = $"{baseUrl}/v1/speech:recognize?key={Uri.EscapeDataString(settings.ApiKey)}";

            var body = new GoogleSpeechRequest
            {
                Config = new GoogleSpeechConfig
                {
                    Encoding = "LINEAR16",
                    SampleRateHertz = audio.SampleRate,
                    LanguageCode = string.IsNullOrEmpty(settings.Language) ? "zh-CN" : settings.Language,
                },
                Audio = new GoogleSpeechAudio
                {
                    Content = Convert.ToBase64String(wavBytes),
                },
            };

            var result = await SendJsonAsync(endpoint, body, settings, cancellationToken,
                truncateLen: 240);
            if (!result.IsSuccess)
            {
                return new SttResult { Error = result.Error };
            }

            var response = ParseResponseJson<GoogleSpeechResponse>(result);
            if (response == null)
            {
                return new SttResult { Error = string.Format(Locales.STT_RESPONSE_PARSE_FAILED.McsLocalized(), ProviderDisplayName) };
            }
            var sb = new StringBuilder();
            if (response.Results is { Count: > 0 })
            {
                foreach (var item in response.Results)
                {
                    var transcript = item?.Alternatives?.FirstOrDefault()?.Transcript;
                    if (!string.IsNullOrWhiteSpace(transcript))
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(' ');
                        }
                        sb.Append(transcript);
                    }
                }
            }

            return new SttResult
            {
                Text = sb.ToString(),
                DetectedLanguage = settings.Language,
            };
        }
    }
}
