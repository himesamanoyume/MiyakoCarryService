using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

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
            if (!TryPrepareWav(audio, out var wavBytes, out var prepareError))
            {
                return new SttResult { Error = prepareError };
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

            HttpRequestMessage request = null;
            try
            {
                request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = form,
                };
                // 本地端点无需 ApiKey，为空时不附加 Authorization
                if (!string.IsNullOrEmpty(settings.ApiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                }

                var result = await SendAsync(request, settings, cancellationToken, truncateLen: 240);
                if (!result.IsSuccess)
                {
                    return new SttResult { Error = result.Error };
                }

                var json = ParseResponseJson(result);
                if (json == null)
                {
                    return new SttResult { Error = $"{ProviderTag} 异常：响应解析失败" };
                }
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
                    request?.Dispose();
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
