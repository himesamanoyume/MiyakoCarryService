using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>腾讯云 ASR 一句话识别 REST：签名 + Base64 PCM 上传。占位实装。</summary>
    internal sealed class TencentAsrProvider : ISttProvider
    {
        public async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new SttResult { Error = "TencentAsrProvider：需 TC3-HMAC-SHA256 签名与 SentenceRecognize 实现落地（占位）" };
        }
    }
}