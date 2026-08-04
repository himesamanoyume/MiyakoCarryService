using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>
    /// STT 服务商适配器接口。所有服务商共享同一份 <see cref="ProviderSettings"/> 配置项；
    /// 输入为已编码的 16kHz mono PCM 样本，输出为转写后的自然语言文本。
    /// </summary>
    internal interface ISttProvider
    {
        Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken);
    }
}