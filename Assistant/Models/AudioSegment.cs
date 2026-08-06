namespace MiyakoCarryService.Assistant.Models
{
    /// <summary>
    /// 录音样本：单声道 44.1kHz 16-bit PCM 音频帧。Assistant 端到端管线流转的内部格式。
    /// </summary>
    public sealed class AudioSegment
    {
        /// <summary>归一化后 [-1,1] 的样本数组（与 Unity AudioClip 同格式）。</summary>
        public float[] Samples;

        /// <summary>采样率（Assistant 录音为 44100Hz）。</summary>
        public int SampleRate;

        public int Channels;

        public int LengthSamples => Samples?.Length ?? 0;

        public float DurationSeconds => SampleRate > 0 ? (float)LengthSamples / SampleRate : 0f;
    }
}