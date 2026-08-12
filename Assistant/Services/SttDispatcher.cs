using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Interfaces;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Providers.Stt;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Assistant.Services
{
    internal sealed class SttDispatcher
    {
        private readonly ISttProvider _provider;

        public SttDispatcher(ESttProvider type)
        {
            _provider = type switch
            {
                ESttProvider.OpenAICompatible => new OpenAICompatibleProvider(),
                ESttProvider.AzureSpeech => new AzureSpeechProvider(),
                ESttProvider.GoogleSpeech => new GoogleSpeechProvider(),
                ESttProvider.AliyunNls => new AliyunNlsProvider(),
                ESttProvider.TencentAsr => new TencentAsrProvider(),
                ESttProvider.XfyunIat => new XfyunIatProvider(),
                ESttProvider.VolcIat => new VolcIatProvider(),
                ESttProvider.BaiduAsr => new BaiduAsrProvider(),
                _ => null,
            };
        }

        public async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (_provider == null)
            {
                return new SttResult { Error = Locales.STT_PROVIDER_NOT_CONFIGURED.McsLocalized() };
            }
            return await _provider.TranscribeAsync(audio, settings, cancellationToken).ConfigureAwait(false);
        }
    }
}