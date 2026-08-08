using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>
    /// 火山引擎 一句话识别 REST：<c>POST /api/v1/auc/get_one_sentence_recognition</c>。
    /// 鉴权：query 携带 appid/token/signature/ts，signature = MD5("appid={appid}&token={token}&ts={ts}")。
    /// ApiKey = AppID，ApiSecret = AccessToken。强制 16kHz，raw 音频上传，响应 result 为 base64 文本。
    /// </summary>
    public sealed class VolcIatProvider : BaseSttProvider
    {
        private const string DefaultBaseUrl = "https://openspeech.bytedance.com";
        private const int RequiredRate = 16000;

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (audio == null || audio.LengthSamples == 0)
            {
                return new SttResult { Error = "AudioSegment 为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey) || string.IsNullOrEmpty(settings.ApiSecret))
            {
                return new SttResult { Error = "火山需填写 SttApiKey（AppID）与 SttApiSecret（AccessToken）" };
            }

            var rate = audio.SampleRate;
            var samples = audio.Samples;
            if (rate != RequiredRate)
            {
                samples = Tools.Resample(samples, rate, RequiredRate);
                rate = RequiredRate;
            }
            var wavBytes = Tools.Encode(samples, rate, 1);
            if (wavBytes.Length == 0)
            {
                return new SttResult { Error = "WAV 编码失败" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var client = AssistantHttpClient.WithTimeout();
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                var signature = Md5($"appid={settings.ApiKey}&token={settings.ApiSecret}&ts={ts}");
                var endpoint = $"{baseUrl}/api/v1/auc/get_one_sentence_recognition" +
                    $"?appid={Uri.EscapeDataString(settings.ApiKey)}&token={Uri.EscapeDataString(settings.ApiSecret)}&signature={signature}&ts={ts}";

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new ByteArrayContent(wavBytes),
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new SttResult { Error = $"火山 HTTP {response.StatusCode}: {SafeTrim(responseString, 240)}" };
                }

                var json = JObject.Parse(responseString);
                var code = json.Value<int>("code");
                if (code != 0)
                {
                    return new SttResult { Error = $"火山识别失败 {code}: {json.Value<string>("message") ?? "未知错误"}" };
                }

                var resultBase64 = json.Value<string>("result");
                if (string.IsNullOrEmpty(resultBase64))
                {
                    return new SttResult { Text = string.Empty, DetectedLanguage = settings.Language };
                }
                try
                {
                    var text = Encoding.UTF8.GetString(Convert.FromBase64String(resultBase64));
                    return new SttResult { Text = text, DetectedLanguage = settings.Language };
                }
                catch (Exception ex)
                {
                    return new SttResult { Error = $"火山结果解码失败：{ex.Message}" };
                }
            }
            catch (OperationCanceledException)
            {
                return new SttResult { Error = "火山请求超时" };
            }
            catch (Exception ex)
            {
                return new SttResult { Error = $"火山异常：{ex.Message}" };
            }
        }

        private static string Md5(string input)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
