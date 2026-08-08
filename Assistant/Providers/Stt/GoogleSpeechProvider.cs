using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>
    /// Google Cloud Speech-to-Text REST 一句话识别：
    /// <c>POST /v1/speech:recognize?key={ApiKey}</c>，JSON 携带 LINEAR16 base64 音频。
    /// BaseUrl 留空用官方端点，可覆盖为自建代理/中转。
    /// </summary>
    public sealed class GoogleSpeechProvider : BaseSttProvider
    {
        private const string DefaultBaseUrl = "https://speech.googleapis.com";

        protected override string ProviderTag
        {
            get { return "Google"; }
        }

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new SttResult { Error = "SttApiKey 未填写（Google API Key）" };
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
                return new SttResult { Error = $"{ProviderTag} 异常：响应解析失败" };
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
