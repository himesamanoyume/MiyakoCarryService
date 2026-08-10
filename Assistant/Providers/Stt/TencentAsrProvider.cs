using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>
    /// 腾讯云 ASR 一句话识别（SentenceRecognition，TC3-HMAC-SHA256 签名）。
    /// SecretId = ApiKey，SecretKey = ApiSecret。强制 16kHz WAV，base64 提交。
    /// </summary>
    public sealed class TencentAsrProvider : BaseSttProvider
    {
        protected override string ProviderDisplayName => Locales.STTPROVIDERTENCENTASR.McsLocalized();

        private const string DefaultBaseUrl = "https://asr.tencentcloudapi.com";

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings?.ApiKey) || string.IsNullOrEmpty(settings.ApiSecret))
            {
                return new SttResult { Error = Locales.STT_TENCENT_REQUIRED.McsLocalized() };
            }
            if (!TryPrepare16kWav(audio, out var wavBytes, out var prepareError))
            {
                return new SttResult { Error = prepareError };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var host = new Uri(baseUrl).Host;

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
            };
            var bodyString = body.ToString();

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var authorization = BuildAuthorization(host, bodyString, timestamp, date, settings.ApiKey, settings.ApiSecret);

            var result = await SendJsonAsync(baseUrl, body, settings, cancellationToken,
                request =>
                {
                    request.Headers.Add("Host", host);
                    request.Headers.Add("X-TC-Action", "SentenceRecognition");
                    request.Headers.Add("X-TC-Version", "2019-06-14");
                    request.Headers.Add("X-TC-Timestamp", timestamp.ToString());
                    request.Headers.Add("X-TC-Region", "ap-guangzhou");
                    request.Headers.TryAddWithoutValidation("Authorization", authorization);
                },
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
