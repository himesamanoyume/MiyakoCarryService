

using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Interfaces;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    public abstract class BaseSttProvider : BaseProvider, ISttProvider
    {
        /// <summary>多数 STT 服务商强制要求的采样率。</summary>
        protected const int RequiredRate = 16000;

        /// <summary>
        /// 将响应原文解析为 JSON。响应非合法 JSON 时返回 null（视为该厂商异常，错误文案统一为 "{Tag} 异常：响应解析失败"）。
        /// </summary>
        protected JObject ParseResponseJson(PostResponse result)
        {
            try
            {
                return JObject.Parse(result.ResponseText);
            }
            catch
            {
                return null;
            }
        }

        public virtual Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SttResult { Error = "此接口未实现" });
        }

        /// <summary>
        /// 校验音频段非空。通过返回 null；失败返回携带错误信息的 <see cref="SttResult"/>。
        /// </summary>
        protected SttResult ValidateAudio(AudioSegment audio)
        {
            if (audio == null || audio.LengthSamples == 0)
            {
                return new SttResult { Error = "AudioSegment 为空" };
            }
            return null;
        }

        /// <summary>
        /// 按音频原始采样率/声道编码为 WAV。
        /// </summary>
        protected bool TryPrepareWav(AudioSegment audio, out byte[] wavBytes, out string error)
        {
            var invalid = ValidateAudio(audio);
            if (invalid != null)
            {
                wavBytes = null;
                error = invalid.Error;
                return false;
            }

            wavBytes = Tools.Encode(audio.Samples, audio.SampleRate, audio.Channels);
            if (wavBytes.Length == 0)
            {
                wavBytes = null;
                error = "WAV 编码失败";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 强制 16kHz 单声道：采样率不一致时先线性重采样，再编码为 WAV。
        /// </summary>
        protected bool TryPrepare16kWav(AudioSegment audio, out byte[] wavBytes, out string error)
        {
            var invalid = ValidateAudio(audio);
            if (invalid != null)
            {
                wavBytes = null;
                error = invalid.Error;
                return false;
            }

            var rate = audio.SampleRate;
            var samples = audio.Samples;
            if (rate != RequiredRate)
            {
                samples = Tools.Resample(samples, rate, RequiredRate);
                rate = RequiredRate;
            }
            wavBytes = Tools.Encode(samples, rate, 1);
            if (wavBytes.Length == 0)
            {
                wavBytes = null;
                error = "WAV 编码失败";
                return false;
            }

            error = null;
            return true;
        }
    }
}
