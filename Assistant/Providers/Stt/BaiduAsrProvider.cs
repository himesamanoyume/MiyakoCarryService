using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>百度智能云语音识别 一句话 REST：API Key/Secret 换 Token + Base64 上传。占位实装。</summary>
    internal sealed class BaiduAsrProvider : ISttProvider
    {
        public async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new SttResult { Error = "BaiduAsrProvider：需 OAuth Token 与 REST 一句话识别实现落地（占位）" };
        }
    }
}