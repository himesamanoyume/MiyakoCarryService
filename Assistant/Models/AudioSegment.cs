namespace MiyakoCarryService.Assistant.Models
{
    public class AudioSegment
    {
        public float[] Samples;
        public int SampleRate;
        public int Channels;
        public int LengthSamples => Samples?.Length ?? 0;
        public float DurationSeconds => SampleRate > 0 ? (float)LengthSamples / SampleRate : 0f;
    }
}