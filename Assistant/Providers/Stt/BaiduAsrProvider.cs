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
    /// 百度智能云 短语音识别一句话 REST：
    /// 先用 client_credentials 换取 access_token，再 POST <c>/server_api</c> 提交 base64 WAV（强制 16kHz）。
    /// ApiKey = client_id，ApiSecret = client_secret。
    /// </summary>
    public sealed class BaiduAsrProvider : BaseSttProvider
    {
        private const string DefaultBaseUrl = "https://vop.baidubce.com";

        protected override string ProviderTag
        {
            get { return "百度"; }
        }

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings?.ApiKey) || string.IsNullOrEmpty(settings.ApiSecret))
            {
                return new SttResult { Error = "百度识别需填写 SttApiKey（client_id）与 SttApiSecret（client_secret）" };
            }
            if (!TryPrepare16kWav(audio, out var wavBytes, out var prepareError))
            {
                return new SttResult { Error = prepareError };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var client = AssistantHttpClient.WithTimeout();

            // 1. 换取 access_token
            using (var tokenCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                tokenCts.CancelAfter(TimeSpan.FromSeconds(15));
                var token = await FetchAccessTokenAsync(client, settings, tokenCts.Token).ConfigureAwait(false);
                if (token == null)
                {
                    return new SttResult { Error = "百度 access_token 换取失败（请检查 ApiKey/ApiSecret）" };
                }

                // 2. 一句话识别
                var body = new JObject
                {
                    ["format"] = "wav",
                    ["rate"] = RequiredRate,
                    ["channel"] = 1,
                    ["cuid"] = "miyako-carry-service",
                    ["token"] = token,
                    ["speech"] = Convert.ToBase64String(wavBytes),
                };

                var result = await SendJsonAsync($"{baseUrl}/server_api", body, settings, cancellationToken,
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
                var errNo = json.Value<int>("err_no");
                if (errNo != 0)
                {
                    return new SttResult { Error = $"百度识别失败 {errNo}: {json.Value<string>("err_msg") ?? "未知错误"}" };
                }
                var text = json["result"]?[0]?.ToString() ?? string.Empty;
                return new SttResult { Text = text, DetectedLanguage = settings.Language };
            }
        }

        private static async Task<string> FetchAccessTokenAsync(HttpClient client, ProviderSettings settings, CancellationToken ct)
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
