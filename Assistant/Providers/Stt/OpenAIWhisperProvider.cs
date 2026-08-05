using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
    internal sealed class OpenAIWhisperProvider : ISttProvider
    {
        public async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (audio == null || audio.LengthSamples == 0)
            {
                return new SttResult { Error = "AudioSegment 为空" };
            }

            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new SttResult { Error = "SttApiKey 未填写" };
            }

            var wavBytes = WavEncoder.Encode(audio.Samples, audio.SampleRate, audio.Channels);
            if (wavBytes.Length == 0)
            {
                return new SttResult { Error = "WAV 编码失败" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? "https://api.openai.com/v1" : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? "whisper-1" : settings.ModelId;

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(wavBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(fileContent, "file", "voice.wav");
            form.Add(new StringContent(model), "model");
            if (!string.IsNullOrEmpty(settings.Language))
            {
                form.Add(new StringContent(settings.Language), "language");
            }
            form.Add(new StringContent("json"), "response_format");

            var client = AssistantHttpClient.WithTimeout(settings);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/audio/transcriptions")
            {
                Content = form,
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            try
            {
                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
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
        }

        private static string SafeTrim(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }
    }
}