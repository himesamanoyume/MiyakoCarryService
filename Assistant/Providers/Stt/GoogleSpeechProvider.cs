using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>
    /// Google Cloud Speech-to-Text REST <c>/speech:recognize</c>：服务账号 JWT 签名 + LINEAR16 base64 上传。占位实装。
    /// </summary>
    internal sealed class GoogleSpeechProvider : ISttProvider
    {
        public async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new SttResult { Error = "GoogleSpeechProvider：需 OAuth JWT 与 LINEAR16 REST 实现落地（占位）" };
        }
    }
}