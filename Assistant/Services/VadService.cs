using System;

namespace MiyakoCarryService.Assistant.Services
{
    /// <summary>
    /// 简易 RMS 能量阈值 VAD 实现供 FreeTalk 模式自动起止录音使用。无外部依赖。
    /// </summary>
    internal sealed class VadService
    {
        private readonly VadParams _params;

        public VadService(VadParams @params)
        {
            _params = @params;
        }

        /// <summary>给定原始样本块计算 RMS 能量。</summary>
        public float ComputeRms(float[] samples)
        {
            if (samples == null || samples.Length == 0) { return 0f; }
            double sumSq = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                var v = samples[i];
                sumSq += v * v;
            }
            return (float)Math.Sqrt(sumSq / samples.Length);
        }

        /// <summary>根据当前块的 RMS 判断是否处于"语音段"。</summary>
        public bool IsSpeech(float rms)
        {
            return rms >= _params.EnergyThreshold;
        }

        /// <summary>持续静默是否长时间超过 SilenceSeconds，可结束录音。</summary>
        public bool ShouldStopAfterSilence(float currentRms, float silenceSeconds)
        {
            return !IsSpeech(currentRms) && silenceSeconds >= _params.SilenceSeconds;
        }
    }

    internal sealed class VadParams
    {
        public float EnergyThreshold = 0.02f;
        public float SilenceSeconds = 1.2f;
    }
}