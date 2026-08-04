using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    /// <summary>
    /// 阿里云语音识别 一句话识别 (REST 简化版)：兼容大小不超过 60 秒的 16kHz WAV 上传，
    /// 返回 <c>{"result":["..."]}</c>。地域/TokenBase 由 <c>BaseUrl</c> 指定可替代默认深圳端点。
    /// 完整签名串算法留给后续按官方最新 SDK 落地；此处保留端点/参数/UI 兼容。
    /// </summary>
    internal sealed class AliyunNlsProvider : ISttProvider
    {
        public async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (audio == null || audio.LengthSamples == 0) return new SttResult { Error = "AudioSegment 为空" };
            if (string.IsNullOrEmpty(settings?.ApiKey)) return new SttResult { Error = "SttApiKey 未填写（阿里云 AccessKey 或 NLS Token）" };

            // 阿里云一句话识别 REST：完整实现需 NLS-SDK 签名，留为可填充桩。
            return new SttResult { Error = "AliyunNlsProvider：需 NLS SDK 签名实现（占位）" };
        }
    }
}