using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>
    /// 阿里云 NLS REST 一句话识别：
    /// 先用 AccessKey 换取 NLS token（<c>nls-meta</c> 的 token 接口），再 POST <c>stream/v1/asr</c>。
    /// ApiKey = AccessKeyId，ApiSecret = AccessKeySecret，ModelId = appkey。
    /// 强制 16kHz。BaseUrl 可覆盖网关域名（默认上海）。
    /// </summary>
    internal sealed class AliyunNlsProvider : BaseSttProvider
    {
        private const string DefaultGateway = "https://nls-gateway-cn-shanghai.aliyuncs.com";
        private const string DefaultTokenApi = "https://nls-meta.cn-shanghai.aliyuncs.com";
        private const int RequiredRate = 16000;

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (audio == null || audio.LengthSamples == 0)
            {
                return new SttResult { Error = "AudioSegment 为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey) || string.IsNullOrEmpty(settings.ApiSecret))
            {
                return new SttResult { Error = "阿里 NLS 需填写 SttApiKey（AccessKeyId）、SttApiSecret（AccessKeySecret）与 SttModelId（appkey）" };
            }
            if (string.IsNullOrEmpty(settings.ModelId))
            {
                return new SttResult { Error = "阿里 NLS 需在 SttModelId 中填写 appkey" };
            }

            var rate = audio.SampleRate;
            var samples = audio.Samples;
            if (rate != RequiredRate)
            {
                samples = AudioResampler.Resample(samples, rate, RequiredRate);
                rate = RequiredRate;
            }
            var wavBytes = WavEncoder.Encode(samples, rate, 1);
            if (wavBytes.Length == 0)
            {
                return new SttResult { Error = "WAV 编码失败" };
            }

            var gateway = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultGateway : settings.BaseUrl.TrimEnd('/');
            var client = AssistantHttpClient.WithTimeout();
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);

            try
            {
                using var tokenCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                tokenCts.CancelAfter(TimeSpan.FromSeconds(15));
                var token = await FetchTokenAsync(client, settings, tokenCts.Token).ConfigureAwait(false);
                if (string.IsNullOrEmpty(token))
                {
                    return new SttResult { Error = "阿里 NLS token 换取失败（请检查 AccessKeyId/AccessKeySecret）" };
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);
                var endpoint = $"{gateway}/stream/v1/asr?appkey={Uri.EscapeDataString(settings.ModelId)}" +
                    $"&format=wav&sample_rate={RequiredRate}&enable_punctuation_prediction=true&enable_inverse_text_normalization=true";
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new System.Net.Http.ByteArrayContent(wavBytes),
                };
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                request.Headers.Add("X-NLS-AppKey", settings.ModelId);
                request.Headers.Add("X-NLS-Token", token);

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new SttResult { Error = $"阿里 HTTP {response.StatusCode}: {SafeTrim(responseString, 240)}" };
                }

                var json = JObject.Parse(responseString);
                var status = json.Value<int>("status");
                if (status != 20000000)
                {
                    return new SttResult { Error = $"阿里识别失败 {status}: {json.Value<string>("message") ?? "未知错误"}" };
                }
                return new SttResult { Text = json.Value<string>("result") ?? string.Empty, DetectedLanguage = settings.Language };
            }
            catch (OperationCanceledException)
            {
                return new SttResult { Error = "阿里请求超时" };
            }
            catch (Exception ex)
            {
                return new SttResult { Error = $"阿里异常：{ex.Message}" };
            }
        }

        private static async Task<string> FetchTokenAsync(HttpClient client, ProviderSettings settings, CancellationToken ct)
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
