using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>火山引擎 一句话识别 REST：AppID + Token 签名 + Base64 上传。占位实装。</summary>
    internal sealed class VolcIatProvider : ISttProvider
    {
        public async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new SttResult { Error = "VolcIatProvider：需火山引擎一句话识别签名实现落地（占位）" };
        }
    }
}