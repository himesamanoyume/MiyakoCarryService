using System;

namespace MiyakoCarryService.Assistant.Services
{
    /// <summary>
    /// 简易 RMS 能量阈值 VAD 实现供 FreeTalk 模式自动起止录音使用。无外部依赖。
    /// 自适应噪音地板：只统计非语音窗口的 RMS 滑动历史（约 2s），以中位数作为环境噪音基准，
    /// 说话判定阈值 = max(配置下限, 噪音基准 × 倍数)。偶发环境噪音不会反复刷新静音计时，
    /// 说完话后能在静音秒数内及时结束录音并发起 STT 请求。
    /// </summary>
    internal sealed class VadService
    {
        private const float NoiseFloorFactor = 2.0f;
        private const int FloorWindowCount = 40; // 40 × 50ms ≈ 2s 滑动历史

        private readonly VadParams _params;
        private readonly float[] _floorHistory = new float[FloorWindowCount];
        private int _floorHead;
        private bool _floorFull;

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

        /// <summary>
        /// 每窗调用：以当前 RMS 更新噪音地板历史。语音窗口不进入历史（防止地板被抬升）；
        /// 首个入窗样本若恰为语音，按配置下限折算，避免开局把地板抬死。
        /// </summary>
        public void UpdateNoiseFloor(float rms)
        {
            if (rms <= 0f)
            {
                return;
            }
            if (IsSpeech(rms))
            {
                return;
            }
            if (_floorHead == 0 && !_floorFull)
            {
                rms = Math.Min(rms, _params.EnergyThreshold / NoiseFloorFactor);
            }
            _floorHistory[_floorHead] = rms;
            _floorHead = (_floorHead + 1) % FloorWindowCount;
            if (_floorHead == 0)
            {
                _floorFull = true;
            }
        }

        /// <summary>根据当前块的 RMS 判断是否处于"语音段"：需超过配置下限与噪音基准×倍数的较大者。</summary>
        public bool IsSpeech(float rms)
        {
            return rms >= CurrentThreshold;
        }

        /// <summary>当前语音判定阈值（随噪音地板自适应）。</summary>
        public float CurrentThreshold
        {
            get
            {
                if (_floorHead == 0 && !_floorFull)
                {
                    return _params.EnergyThreshold;
                }
                int count = _floorFull ? FloorWindowCount : _floorHead;
                var sorted = new float[count];
                Array.Copy(_floorHistory, sorted, count);
                Array.Sort(sorted);
                float median = sorted[count / 2];
                return Math.Max(_params.EnergyThreshold, median * NoiseFloorFactor);
            }
        }

        public float EnergyThreshold => _params.EnergyThreshold;
        public float SilenceSeconds => _params.SilenceSeconds;

        /// <summary>持续静默是否长时间超过 SilenceSeconds，可结束录音。</summary>
        public bool ShouldStopAfterSilence(float currentRms, float silenceSeconds)
        {
            return !IsSpeech(currentRms) && silenceSeconds >= _params.SilenceSeconds;
        }
    }

    internal sealed class VadParams
    {
        public float EnergyThreshold = 0.02f;
        public float SilenceSeconds = 1f;
    }
}
