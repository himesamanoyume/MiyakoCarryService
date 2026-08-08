using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>
    /// Azure Speech Service 一句话识别 REST：
    /// 端点 <c>https://{region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1</c>，
    /// 以 <c>Ocp-Apim-Subscription-Key</c> 鉴权，WAV 二进制直接上传。
    /// BaseUrl 留空时提示填写（订阅区域未知，无法推断默认区域）。
    /// </summary>
    public sealed class AzureSpeechProvider : BaseSttProvider
    {
        private const string DefaultBaseUrl = "https://eastasia.stt.speech.microsoft.com";

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (audio == null || audio.LengthSamples == 0)
            {
                return new SttResult { Error = "AudioSegment 为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new SttResult { Error = "SttApiKey 未填写（Azure Subscription Key）" };
            }

            var wavBytes = Tools.Encode(audio.Samples, audio.SampleRate, audio.Channels);
            if (wavBytes.Length == 0)
            {
                return new SttResult { Error = "WAV 编码失败" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var endpoint = $"{baseUrl}/speech/recognition/conversation/cognitiveservices/v1" +
                $"?language={Uri.EscapeDataString(string.IsNullOrEmpty(settings.Language) ? "zh-CN" : settings.Language)}&format=simple";

            var client = AssistantHttpClient.WithTimeout();
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new ByteArrayContent(wavBytes),
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav")
                {
                    Parameters = { new NameValueHeaderValue("codecs", "audio/pcm") },
                };
                request.Headers.Add("Ocp-Apim-Subscription-Key", settings.ApiKey);

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new SttResult { Error = $"Azure HTTP {response.StatusCode}: {SafeTrim(responseString, 240)}" };
                }

                var json = JObject.Parse(responseString);
                var status = json.Value<string>("RecognitionStatus");
                if (string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase))
                {
                    return new SttResult
                    {
                        Text = json.Value<string>("DisplayText") ?? string.Empty,
                        DetectedLanguage = settings.Language,
                    };
                }

                return new SttResult { Error = $"Azure 识别状态异常：{status ?? "未知"}" };
            }
            catch (OperationCanceledException)
            {
                return new SttResult { Error = "Azure 请求超时" };
            }
            catch (Exception ex)
            {
                return new SttResult { Error = $"Azure 异常：{ex.Message}" };
            }
        }
    }
}
