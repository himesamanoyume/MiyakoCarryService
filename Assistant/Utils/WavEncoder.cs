using System;
using System.IO;
using System.Text;

namespace MiyakoCarryService.Assistant.Utils
{
    /// <summary>
    /// 将 Assistant 录音得到的 PCM 浮点样本流编码为单声道 16-bit 16kHz WAV 字节流，
    /// 适配 OpenAI Whisper、Azure Speech、阿里云 NLS 等主流 STT REST 接口的 multipart 上传格式。
    /// </summary>
    internal static class WavEncoder
    {
        public static byte[] Encode(float[] samples, int sampleRate = 16000, int channels = 1)
        {
            if (samples == null)
            {
                return Array.Empty<byte>();
            }

            // 16-bit PCM 量化
            var pcm = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                var v = samples[i];
                v = Math.Max(-1f, Math.Min(1f, v));
                pcm[i] = (short)(v < 0 ? v * short.MinValue : v * short.MaxValue);
            }

            var byteRate = sampleRate * channels * 2;
            var blockAlign = (short)(channels * 2);
            var dataSize = pcm.Length * 2;
            var totalSize = 44 + dataSize;

            using var memory = new MemoryStream(totalSize);
            using var writer = new BinaryWriter(memory, Encoding.ASCII);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(totalSize - 8);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            var buffer = new byte[pcm.Length * 2];
            Buffer.BlockCopy(pcm, 0, buffer, 0, buffer.Length);
            writer.Write(buffer);

            return memory.ToArray();
        }
    }
}