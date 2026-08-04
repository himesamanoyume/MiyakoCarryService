using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Providers.Stt;

namespace MiyakoCarryService.Assistant.Services
{
    /// <summary>
    /// STT 服务分发器。根据 <see cref="ESttProvider"/> 选择具体实现，
    /// 配置项通过 <see cref="ProviderSettings"/> 统一传入。
    /// </summary>
    internal sealed class SttDispatcher
    {
        private readonly ISttProvider _provider;

        public SttDispatcher(ESttProvider type)
        {
            _provider = type switch
            {
                ESttProvider.OpenAIWhisper => new OpenAIWhisperProvider(),
                ESttProvider.AzureSpeech   => new AzureSpeechProvider(),
                ESttProvider.GoogleSpeech  => new GoogleSpeechProvider(),
                ESttProvider.AliyunNls     => new AliyunNlsProvider(),
                ESttProvider.TencentAsr    => new TencentAsrProvider(),
                ESttProvider.XfyunIat      => new XfyunIatProvider(),
                ESttProvider.VolcIat       => new VolcIatProvider(),
                ESttProvider.BaiduAsr       => new BaiduAsrProvider(),
                _                          => null,
            };
        }

        public bool IsConfigured => _provider != null;

        public async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (_provider == null)
            {
                return new SttResult { Error = "SttProvider 未配置或未启用" };
            }
            return await _provider.TranscribeAsync(audio, settings, cancellationToken).ConfigureAwait(false);
        }
    }
}