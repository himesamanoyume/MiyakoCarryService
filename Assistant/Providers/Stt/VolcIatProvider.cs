using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Models.Providers;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    public sealed class VolcIatProvider : BaseSttProvider
    {
        protected override string ProviderDisplayName => Locales.STTPROVIDERVOLCIAT.McsLocalized();

        private const string DefaultBaseUrl = "https://openspeech.bytedance.com";

        public override async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings?.ApiKey) || string.IsNullOrEmpty(settings.ApiSecret))
            {
                return new SttResult { Error = Locales.STT_VOLC_REQUIRED.McsLocalized() };
            }
            if (!TryPrepare16kWav(audio, out var wavBytes, out var prepareError))
            {
                return new SttResult { Error = prepareError };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signature = Md5($"appid={settings.ApiKey}&token={settings.ApiSecret}&ts={ts}");
            var endpoint = $"{baseUrl}/api/v1/auc/get_one_sentence_recognition" + $"?appid={Uri.EscapeDataString(settings.ApiKey)}&token={Uri.EscapeDataString(settings.ApiSecret)}&signature={signature}&ts={ts}";

            var result = await SendRawAsync(endpoint, wavBytes, "application/octet-stream", settings, cancellationToken,
                truncateLen: 240);
            if (!result.IsSuccess)
            {
                return new SttResult { Error = result.Error };
            }

            var response = ParseResponseJson<VolcIatResponse>(result);
            if (response == null)
            {
                return new SttResult { Error = string.Format(Locales.STT_RESPONSE_PARSE_FAILED.McsLocalized(), ProviderDisplayName) };
            }
            var code = response.Code ?? 0;
            if (code != 0)
            {
                return new SttResult { Error = $"Error {code}: {response.Message ?? Locales.UNKNOWN_ERROR.McsLocalized()}" };
            }

            var resultBase64 = response.Result;
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
                return new SttResult { Error = string.Format(Locales.STT_DECODE_FAILED.McsLocalized(), ProviderDisplayName, ex.Message) };
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
