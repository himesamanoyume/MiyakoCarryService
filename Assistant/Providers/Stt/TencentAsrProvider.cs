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
    /// 腾讯云 ASR 一句话识别（SentenceRecognition，TC3-HMAC-SHA256 签名）。
    /// SecretId = ApiKey，SecretKey = ApiSecret。强制 16kHz WAV，base64 提交。
    /// </summary>
    public sealed class TencentAsrProvider : BaseSttProvider
    {
        private const string DefaultBaseUrl = "https://asr.tencentcloudapi.com";
        private const int RequiredRate = 16000;

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (audio == null || audio.LengthSamples == 0)
            {
                return new SttResult { Error = "AudioSegment 为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey) || string.IsNullOrEmpty(settings.ApiSecret))
            {
                return new SttResult { Error = "腾讯云需填写 SttApiKey（SecretId）与 SttApiSecret（SecretKey）" };
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
                var body = new JObject
                {
                    ["ProjectId"] = 0,
                    ["SubServiceType"] = "short",
                    ["EngSerViceType"] = "16k_zh",
                    ["SourceType"] = 1,
                    ["VoiceFormat"] = "wav",
                    ["Data"] = Convert.ToBase64String(wavBytes),
                    ["FilterDirty"] = 0,
                    ["FilterModal"] = 0,
                    ["ConvertNumMode"] = 1,
                }.ToString();

                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var authorization = BuildAuthorization(host, body, timestamp, date, settings.ApiKey, settings.ApiSecret);

                using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json; charset=utf-8"),
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                request.Headers.Add("Host", host);
                request.Headers.Add("X-TC-Action", "SentenceRecognition");
                request.Headers.Add("X-TC-Version", "2019-06-14");
                request.Headers.Add("X-TC-Timestamp", timestamp.ToString());
                request.Headers.Add("X-TC-Region", "ap-guangzhou");
                request.Headers.TryAddWithoutValidation("Authorization", authorization);

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new SttResult { Error = $"腾讯云 HTTP {response.StatusCode}: {SafeTrim(responseString, 240)}" };
                }

                var json = JObject.Parse(responseString);
                var error = json["Response"]?["Error"];
                if (error != null)
                {
                    return new SttResult { Error = $"腾讯云识别失败 {error["Code"]}: {error["Message"]}" };
                }
                return new SttResult
                {
                    Text = json["Response"]?["Result"]?.ToString() ?? string.Empty,
                    DetectedLanguage = settings.Language,
                };
            }
            catch (OperationCanceledException)
            {
                return new SttResult { Error = "腾讯云请求超时" };
            }
            catch (Exception ex)
            {
                return new SttResult { Error = $"腾讯云异常：{ex.Message}" };
            }
        }

        private string BuildAuthorization(string host, string body, long timestamp, string date, string secretId, string secretKey)
        {
            var payloadHash = Sha256Hex(body);
            var canonicalRequest = $"POST\n/\n\ncontent-type:application/json; charset=utf-8\nhost:{host}\n\ncontent-type;host\n{payloadHash}";
            var stringToSign = $"TC3-HMAC-SHA256\n{timestamp}\n{date}/asr/tc3_request\n{Sha256Hex(canonicalRequest)}";

            byte[] secretDate, secretService, secretSigning;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("TC3" + secretKey)))
            {
                secretDate = hmac.ComputeHash(Encoding.UTF8.GetBytes(date));
            }
            using (var hmac = new HMACSHA256(secretDate))
            {
                secretService = hmac.ComputeHash(Encoding.UTF8.GetBytes("asr"));
            }
            using (var hmac = new HMACSHA256(secretService))
            {
                secretSigning = hmac.ComputeHash(Encoding.UTF8.GetBytes("tc3_request"));
            }

            string signature;
            using (var hmac = new HMACSHA256(secretSigning))
            {
                signature = Hex(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            }
            return $"TC3-HMAC-SHA256 Credential={secretId}/{date}/asr/tc3_request, SignedHeaders=content-type;host, Signature={signature}";
        }

        private static string Sha256Hex(string input)
        {
            using var sha = SHA256.Create();
            return Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        private static string Hex(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 2);
            foreach (var b in data)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
