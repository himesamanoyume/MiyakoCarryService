using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Models.Providers;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    public sealed class OpenAICompatibleProvider : BaseSttProvider
    {
        protected override string ProviderDisplayName => Locales.SttProviderOpenAICompatible.McsLocalized();

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (!TryPrepareWav(audio, out var wavBytes, out var prepareError))
            {
                return new SttResult { Error = prepareError };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? "https://api.openai.com/v1" : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? "whisper-1" : settings.ModelId;
            var endpoint = baseUrl.EndsWith("/audio/transcriptions", StringComparison.OrdinalIgnoreCase) ? baseUrl : $"{baseUrl}/audio/transcriptions";

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
                if (!string.IsNullOrEmpty(settings.ApiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                }

                var result = await SendAsync(request, settings, cancellationToken, truncateLen: 240);
                if (!result.IsSuccess)
                {
                    return new SttResult { Error = result.Error };
                }

                var response = ParseResponseJson<OpenAiSttResponse>(result);
                if (response == null)
                {
                    return new SttResult { Error = string.Format(Locales.STT_RESPONSE_PARSE_FAILED.McsLocalized(), ProviderDisplayName) };
                }
                return new SttResult
                {
                    Text = response.Text ?? string.Empty,
                    DetectedLanguage = settings.Language,
                };
            }
            catch (OperationCanceledException)
            {
                return new SttResult { Error = string.Format(Locales.HTTP_REQUEST_TIMEOUT.McsLocalized(), ProviderDisplayName) };
            }
            catch (Exception ex)
            {
                return new SttResult { Error = string.Format(Locales.HTTP_EXCEPTION.McsLocalized(), ProviderDisplayName, ex.Message) };
            }
            finally
            {
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
