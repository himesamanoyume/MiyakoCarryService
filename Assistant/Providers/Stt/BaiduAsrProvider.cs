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
    /// 百度智能云 短语音识别一句话 REST：
    /// 先用 client_credentials 换取 access_token，再 POST <c>/server_api</c> 提交 base64 WAV（强制 16kHz）。
    /// ApiKey = client_id，ApiSecret = client_secret。
    /// </summary>
    internal sealed class BaiduAsrProvider : BaseSttProvider
    {
        private const string DefaultBaseUrl = "https://vop.baidubce.com";
        private const int RequiredRate = 16000;

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (audio == null || audio.LengthSamples == 0)
            {
                return new SttResult { Error = "AudioSegment 为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey) || string.IsNullOrEmpty(settings.ApiSecret))
            {
                return new SttResult { Error = "百度识别需填写 SttApiKey（client_id）与 SttApiSecret（client_secret）" };
            }

            // 百度强制 16kHz：录音为 44.1kHz 时降采样
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

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var client = AssistantHttpClient.WithTimeout();
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);

            try
            {
                // 1. 换取 access_token
                using (var tokenCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    tokenCts.CancelAfter(TimeSpan.FromSeconds(15));
                    var token = await FetchAccessTokenAsync(baseUrl, client, settings, tokenCts.Token).ConfigureAwait(false);
                    if (token == null)
                    {
                        return new SttResult { Error = "百度 access_token 换取失败（请检查 ApiKey/ApiSecret）" };
                    }

                    // 2. 一句话识别
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(timeout);
                    var body = new JObject
                    {
                        ["format"] = "wav",
                        ["rate"] = RequiredRate,
                        ["channel"] = 1,
                        ["cuid"] = "miyako-carry-service",
                        ["token"] = token,
                        ["speech"] = Convert.ToBase64String(wavBytes),
                    };

                    using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/server_api")
                    {
                        Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json"),
                    };
                    using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        return new SttResult { Error = $"百度 HTTP {response.StatusCode}: {SafeTrim(responseString, 240)}" };
                    }

                    var json = JObject.Parse(responseString);
                    var errNo = json.Value<int>("err_no");
                    if (errNo != 0)
                    {
                        return new SttResult { Error = $"百度识别失败 {errNo}: {json.Value<string>("err_msg") ?? "未知错误"}" };
                    }
                    var result = json["result"]?[0]?.ToString() ?? string.Empty;
                    return new SttResult { Text = result, DetectedLanguage = settings.Language };
                }
            }
            catch (OperationCanceledException)
            {
                return new SttResult { Error = "百度请求超时" };
            }
            catch (Exception ex)
            {
                return new SttResult { Error = $"百度异常：{ex.Message}" };
            }
        }

        private static async Task<string> FetchAccessTokenAsync(string baseUrl, HttpClient client, ProviderSettings settings, CancellationToken ct)
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
