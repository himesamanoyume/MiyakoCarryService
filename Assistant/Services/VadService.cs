using System;

namespace MiyakoCarryService.Assistant.Services
{
    internal sealed class VadService
    {
        private const float NoiseFloorFactor = 2.0f;
        private const int FloorWindowCount = 40;

        private readonly VadParams _params;
        private readonly float[] _floorHistory = new float[FloorWindowCount];
        private int _floorHead;
        private bool _floorFull;

        public VadService(VadParams @params)
        {
            _params = @params;
        }

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

        public bool IsSpeech(float rms)
        {
            return rms >= CurrentThreshold;
        }

        public float CurrentThreshold
        {
            get
            {
                if (_floorHead == 0 && !_floorFull)
                {
                    return _params.EnergyThreshold;
                }
                var count = _floorFull ? FloorWindowCount : _floorHead;
                var sorted = new float[count];
                Array.Copy(_floorHistory, sorted, count);
                Array.Sort(sorted);
                var median = sorted[count / 2];
                return Math.Max(_params.EnergyThreshold, median * NoiseFloorFactor);
            }
        }

        public float EnergyThreshold => _params.EnergyThreshold;
        public float SilenceSeconds => _params.SilenceSeconds;

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
