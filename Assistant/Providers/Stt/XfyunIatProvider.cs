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
    /// 讯飞 一句话识别 REST：<c>POST https://iat-api.xfyun.cn/v2/iat</c>。
    /// 鉴权：HMAC-SHA256 生成 authorization 头（ApiKey=apiKey，ApiSecret=apiSecret，ModelId=app_id）。
    /// 强制 16kHz；响应文本在 data.result.rg[].v（base64）中按序拼接。
    /// </summary>
    public sealed class XfyunIatProvider : BaseSttProvider
    {
        private const string DefaultBaseUrl = "https://iat-api.xfyun.cn";
        private const int RequiredRate = 16000;

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (audio == null || audio.LengthSamples == 0)
            {
                return new SttResult { Error = "AudioSegment 为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey) || string.IsNullOrEmpty(settings.ApiSecret))
            {
                return new SttResult { Error = "讯飞需填写 SttApiKey（apiKey）与 SttApiSecret（apiSecret）" };
            }
            if (string.IsNullOrEmpty(settings.ModelId))
            {
                return new SttResult { Error = "讯飞需在 SttModelId 中填写 app_id" };
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
            var host = new Uri(baseUrl).Host;
            var client = AssistantHttpClient.WithTimeout();
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                var date = DateTime.UtcNow.ToString("R");
                var authorization = BuildAuthorization(host, date, settings.ApiKey, settings.ApiSecret);

                var body = new JObject
                {
                    ["common"] = new JObject { ["app_id"] = settings.ModelId },
                    ["business"] = new JObject
                    {
                        ["aue"] = "raw",
                        ["auf"] = $"audio/L16;rate={RequiredRate}",
                        ["vad_eos"] = 3000,
                        ["domain"] = "iat",
                        ["language"] = string.IsNullOrEmpty(settings.Language) ? "zh_cn" : settings.Language,
                    },
                    ["data"] = new JObject
                    {
                        ["audio"] = Convert.ToBase64String(wavBytes),
                        ["sample_rate"] = RequiredRate,
                    },
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/iat")
                {
                    Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json"),
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Headers.Add("Authorization", authorization);
                request.Headers.Add("Date", date);

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new SttResult { Error = $"讯飞 HTTP {response.StatusCode}: {SafeTrim(responseString, 240)}" };
                }

                var json = JObject.Parse(responseString);
                var code = json.Value<int>("code");
                if (code != 0)
                {
                    return new SttResult { Error = $"讯飞识别失败 {code}: {json.Value<string>("message") ?? "未知错误"}" };
                }

                var sb = new StringBuilder();
                if (json["data"]?["result"]?["rg"] is JArray rg)
                {
                    foreach (var item in rg)
                    {
                        var v = item?["v"]?.ToString();
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
            catch (OperationCanceledException)
            {
                return new SttResult { Error = "讯飞请求超时" };
            }
            catch (Exception ex)
            {
                return new SttResult { Error = $"讯飞异常：{ex.Message}" };
            }
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
