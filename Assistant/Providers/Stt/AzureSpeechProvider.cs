using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

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

        protected override string ProviderTag
        {
            get { return "Azure"; }
        }

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new SttResult { Error = "SttApiKey 未填写（Azure Subscription Key）" };
            }
            if (!TryPrepareWav(audio, out var wavBytes, out var prepareError))
            {
                return new SttResult { Error = prepareError };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var endpoint = $"{baseUrl}/speech/recognition/conversation/cognitiveservices/v1" +
                $"?language={Uri.EscapeDataString(string.IsNullOrEmpty(settings.Language) ? "zh-CN" : settings.Language)}&format=simple";

            var result = await SendRawAsync(endpoint, wavBytes, "audio/wav", settings, cancellationToken,
                request =>
                {
                    request.Content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("codecs", "audio/pcm"));
                    request.Headers.Add("Ocp-Apim-Subscription-Key", settings.ApiKey);
                },
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
    }
}
