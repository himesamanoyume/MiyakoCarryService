using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;
using Newtonsoft.Json.Linq;

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

            var body = new JObject
            {
                ["config"] = new JObject
                {
                    ["encoding"] = "LINEAR16",
                    ["sampleRateHertz"] = audio.SampleRate,
                    ["languageCode"] = string.IsNullOrEmpty(settings.Language) ? "zh-CN" : settings.Language,
                },
                ["audio"] = new JObject
                {
                    ["content"] = Convert.ToBase64String(wavBytes),
                },
            };

            var result = await SendJsonAsync(endpoint, body, settings, cancellationToken,
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
            var sb = new StringBuilder();
            if (json["results"] is JArray results)
            {
                foreach (var item in results)
                {
                    var transcript = item?["alternatives"]?[0]?["transcript"]?.ToString();
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
