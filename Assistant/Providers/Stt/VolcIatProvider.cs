using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

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

        protected override string ProviderTag
        {
            get { return "火山"; }
        }

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings?.ApiKey) || string.IsNullOrEmpty(settings.ApiSecret))
            {
                return new SttResult { Error = "火山需填写 SttApiKey（AppID）与 SttApiSecret（AccessToken）" };
            }
            if (!TryPrepare16kWav(audio, out var wavBytes, out var prepareError))
            {
                return new SttResult { Error = prepareError };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signature = Md5($"appid={settings.ApiKey}&token={settings.ApiSecret}&ts={ts}");
            var endpoint = $"{baseUrl}/api/v1/auc/get_one_sentence_recognition" +
                $"?appid={Uri.EscapeDataString(settings.ApiKey)}&token={Uri.EscapeDataString(settings.ApiSecret)}&signature={signature}&ts={ts}";

            var result = await SendRawAsync(endpoint, wavBytes, "application/octet-stream", settings, cancellationToken,
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
