using System;

namespace MiyakoCarryService.Assistant.Utils
{
    /// <summary>
    /// 简单线性插值重采样器：把浮点 PCM 样本从源采样率转换到目标采样率。
    /// 用于需要 16kHz 输入的 STT 服务商（百度/腾讯/讯飞/火山/阿里 NLS）在 44.1kHz 录音下的降采样。
    /// </summary>
    internal static class AudioResampler
    {
        /// <summary>
        /// 重采样到目标采样率。目标采样率与源相同或更低时直接返回原数组。
        /// </summary>
        public static float[] Resample(float[] samples, int sourceRate, int targetRate)
        {
            if (samples == null || samples.Length == 0)
            {
                return samples ?? Array.Empty<float>();
            }
            if (sourceRate <= 0 || targetRate <= 0 || sourceRate == targetRate)
            {
                return samples;
            }

            var ratio = (double)targetRate / sourceRate;
            var outLength = (int)Math.Max(1, Math.Round(samples.Length * ratio));
            var output = new float[outLength];
            for (int i = 0; i < outLength; i++)
            {
                var pos = i / ratio;
                var index = (int)pos;
                var frac = (float)(pos - index);
                if (index >= samples.Length - 1)
                {
                    output[i] = samples[samples.Length - 1];
                }
                else
                {
                    output[i] = samples[index] + (samples[index + 1] - samples[index]) * frac;
                }
            }
            return output;
        }
    }
}
