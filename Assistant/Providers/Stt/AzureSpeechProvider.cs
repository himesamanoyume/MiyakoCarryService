using System;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>
    /// Azure Speech Service 一句话识别 REST 简化版：6 端点 <c>https://{region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1</c>。
    /// 完整 streaming WebSocket 路径留为后续按官方 SDK 落地。
    /// </summary>
    internal sealed class AzureSpeechProvider : ISttProvider
    {
        public async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (audio == null || audio.LengthSamples == 0) return new SttResult { Error = "AudioSegment 为空" };
            if (string.IsNullOrEmpty(settings?.ApiKey)) return new SttResult { Error = "SttApiKey 未填写（Azure Subscription Key）" };

            // Azure REST 一句话识别需 attachment WAV 作为二进制载荷，完整实现留为可填充桩。
            await Task.Yield();
            return new SttResult { Error = "AzureSpeechProvider：需完整 REST 一句话识别实现（占位）" };
        }
    }
}