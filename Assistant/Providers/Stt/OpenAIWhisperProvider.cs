using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>
    /// OpenAI Whisper / 任何兼容 <c>/v1/audio/transcriptions</c> 的服务商（如 Groq Whisper、Deepgram 兼容端点等）。
    /// 多部分表单上传 WAV，预期 JSON 响应 <c>{"text":"..."}</c>。
    /// </summary>
    public sealed class OpenAIWhisperProvider : BaseSttProvider
    {
        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (audio == null || audio.LengthSamples == 0)
            {
                return new SttResult { Error = "AudioSegment 为空" };
            }

            var wavBytes = Tools.Encode(audio.Samples, audio.SampleRate, audio.Channels);
            if (wavBytes.Length == 0)
            {
                return new SttResult { Error = "WAV 编码失败" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? "https://api.openai.com/v1" : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? "whisper-1" : settings.ModelId;
            // 兼容 BaseUrl 已填写完整 /audio/transcriptions 端点的情况（如本地服务），避免重复拼接
            var endpoint = baseUrl.EndsWith("/audio/transcriptions", StringComparison.OrdinalIgnoreCase)
                ? baseUrl
                : $"{baseUrl}/audio/transcriptions";

            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(wavBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(fileContent, "file", "voice.wav");
            form.Add(new StringContent(model), "model");
            if (!string.IsNullOrEmpty(settings.Language))
            {
                form.Add(new StringContent(settings.Language), "language");
            }
            form.Add(new StringContent("json"), "response_format");

            var client = AssistantHttpClient.WithTimeout();
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = form,
            };
            // 本地端点无需 ApiKey，为空时不附加 Authorization
            if (!string.IsNullOrEmpty(settings.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            }

            // 请求级超时：按 SttTimeoutSec 精确生效，不受其它请求（如 LLM 调试）干扰
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new SttResult { Error = $"Whisper HTTP {response.StatusCode}: {SafeTrim(responseString, 240)}" };
                }

                var json = JObject.Parse(responseString);
                return new SttResult
                {
                    Text = json.Value<string>("text") ?? string.Empty,
                    DetectedLanguage = settings.Language,
                };
            }
            catch (OperationCanceledException)
            {
                return new SttResult { Error = "Whisper 请求超时" };
            }
            catch (Exception ex)
            {
                return new SttResult { Error = $"Whisper 异常：{ex.Message}" };
            }
            finally
            {
                // Mono 的 MultipartContent.Dispose 存在 NRE 缺陷（请求成功后释放 multipart 表单时崩溃），
                // 释放统一 try/catch 兜住，避免异常吞掉转写结果
                try
                {
                    request.Dispose();
                }
                catch
                {

                }
                try
                {
                    form.Dispose();
                }
                catch
                {

                }
            }
        }
    }
}