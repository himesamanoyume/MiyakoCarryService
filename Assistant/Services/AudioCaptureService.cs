using System;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace MiyakoCarryService.Assistant.Services
{
    /// <summary>
    /// 基于 Unity Microphone 的录音组件。强制 16kHz 单声道 PCM；上层 PushToTalk/FreeTalk 两种模式共用。
    /// </summary>
    internal sealed class AudioCaptureService
    {
        private AudioClip _clip;
        private const int CaptureSampleRate = 16000;
        private const int CaptureChannels = 1;
        private const int LoopSeconds = 60;

        private bool _capturing;

        /// <summary>开始录音。返回时 Unity Microphone 已活跃。</summary>
        public bool Begin()
        {
            if (_capturing) { return true; }
            if (Microphone.devices == null || Microphone.devices.Length == 0) { return false; }

            if (_clip != null)
            {
                UnityEngine.Object.Destroy(_clip);
                _clip = null;
            }

            _clip = Microphone.Start(ResolveMicDeviceName(), true, LoopSeconds, CaptureSampleRate);
            _capturing = _clip != null;
            return _capturing;
        }

        /// <summary>
        /// 解析配置的录音设备名："Default"/空 或设备已不存在（热插拔）时返回 null（系统默认设备）。
        /// </summary>
        private static string ResolveMicDeviceName()
        {
            try
            {
                var device = MiyakoCarryServiceAssistantPlugin.RecordDevice?.Value;
                if (string.IsNullOrEmpty(device) || device == "Default")
                {
                    return null;
                }
                if (Microphone.devices != null && Microphone.devices.Contains(device))
                {
                    return device;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>结束录音。返回最后采样到的浮点样本数组。</summary>
        public float[] End()
        {
            if (!_capturing || _clip == null)
            {
                _capturing = false;
                return Array.Empty<float>();
            }

            int lastSamplePos = 0;
            try
            {
                lastSamplePos = Microphone.GetPosition(null);
            }
            catch
            {
                lastSamplePos = 0;
            }
            _capturing = false;

            if (_clip.samples <= 0)
            {
                UnityEngine.Object.Destroy(_clip);
                _clip = null;
                return Array.Empty<float>();
            }

            // 提取最近的样本（截断到实际已采到的位置，避免填零尾巴）
            int samplesToTake = Math.Min(lastSamplePos, _clip.samples);
            if (samplesToTake <= 0)
            {
                // 无有效采样：跳过 GetData（零长度读取会触发 Unity 原生 unlock 报错），直接清理
                Microphone.End(null);
                UnityEngine.Object.Destroy(_clip);
                _clip = null;
                return Array.Empty<float>();
            }
            var data = new float[samplesToTake];
            try
            {
                _clip.GetData(data, 0);
            }
            catch
            {
                data = Array.Empty<float>();
            }

            Microphone.End(null);
            UnityEngine.Object.Destroy(_clip);
            _clip = null;
            return data;
        }

        public bool IsCapturing => _capturing;

        public int SampleRate => CaptureSampleRate;
        public int Channels => CaptureChannels;

        /// <summary>FreeTalk 模式下 VAD 窗口采样需要的活动 AudioClip。</summary>
        public AudioClip ActiveClip => _clip;
    }
}