using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>
    /// Google Cloud Speech-to-Text REST 一句话识别：
    /// <c>POST /v1/speech:recognize?key={ApiKey}</c>，JSON 携带 LINEAR16 base64 音频。
    /// BaseUrl 留空用官方端点，可覆盖为自建代理/中转。
    /// </summary>
    internal sealed class GoogleSpeechProvider : BaseSttProvider
    {
        private const string DefaultBaseUrl = "https://speech.googleapis.com";

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (audio == null || audio.LengthSamples == 0)
            {
                return new SttResult { Error = "AudioSegment 为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new SttResult { Error = "SttApiKey 未填写（Google API Key）" };
            }

            var wavBytes = WavEncoder.Encode(audio.Samples, audio.SampleRate, audio.Channels);
            if (wavBytes.Length == 0)
            {
                return new SttResult { Error = "WAV 编码失败" };
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

            var client = AssistantHttpClient.WithTimeout();
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json"),
                };

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new SttResult { Error = $"Google HTTP {response.StatusCode}: {SafeTrim(responseString, 240)}" };
                }

                var json = JObject.Parse(responseString);
                var sb = new StringBuilder();
                if (json["results"] is JArray results)
                {
                    foreach (var result in results)
                    {
                        var transcript = result?["alternatives"]?[0]?["transcript"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(transcript))
                        {
                            if (sb.Length > 0) { sb.Append(' '); }
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
            catch (OperationCanceledException)
            {
                return new SttResult { Error = "Google 请求超时" };
            }
            catch (Exception ex)
            {
                return new SttResult { Error = $"Google 异常：{ex.Message}" };
            }
        }
    }
}
