using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>讯飞 一句话识别 REST：HMAC-SHA256 签名 + Base64 上传。占位实装。</summary>
    internal sealed class XfyunIatProvider : ISttProvider
    {
        public async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new SttResult { Error = "XfyunIatProvider：需 HMAC-SHA256 签名与 IAT 一句话接口实现落地（占位）" };
        }
    }
}