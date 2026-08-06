using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiyakoCarryService.Assistant.Services
{
    /// <summary>
    /// 基于 Unity Microphone 的录音组件。默认 44.1kHz 单声道 PCM（贴近硬件原生采样率），
    /// 实际采样率以 <see cref="Microphone"/> 返回的 clip 频率为准（见 <see cref="SampleRate"/>）；
    /// 上层 PushToTalk/FreeTalk 两种模式共用。
    /// 采用 Dissonance / Fika VOIP 相同的"持续会话 + 连续小段轮询"方案：
    /// 麦克风会话只启动/停止一次（<see cref="Begin"/>/<see cref="Stop"/>），录音期间每帧
    /// 通过 <see cref="Poll"/> 以"当前位置差"读取一小段样本累积到内部缓冲，而非在松键时一次性
    /// 对大段 clip 做 GetData。
    /// 注意：<see cref="Microphone.GetPosition"/> 的实际回绕边界由设备硬件 ring buffer 决定
    /// （实测约 1 秒），与 clip 长度无关。回绕时读取"尾段 + 头段"补齐新音频，尾段终点使用
    /// 自适应实测边界 <see cref="_ringSize"/>（观测到的最大位置），绝不能使用 <see cref="AudioClip.samples"/>——
    /// clip 可能远大于实际回绕边界，读它会把历史残留/未写入区段整段读入。
    /// </summary>
    internal sealed class AudioCaptureService
    {
        private AudioClip _clip;
        private const int CaptureSampleRate = 44100;
        private const int CaptureChannels = 1;
        // 循环缓冲：设备实际回绕边界仅约 1s（由硬件决定），10s 缓冲对任何设备都留足余量
        private const int LoopSeconds = 10;

        // 实际启动/读取/停止统一使用的设备名（null = 系统默认设备），避免 Start 与 GetPosition/End 指向不同设备
        private string _deviceName;
        // 实测回绕边界估计（样本数）：首次回绕时由观测位置初始化，之后只增不减；0 = 未知
        private int _ringSize;

        private bool _capturing;
        private int _lastReadPos;
        private readonly List<float> _samples = new List<float>();

        /// <summary>开始一段录音。麦克风未运行时启动，并重置本段累积缓冲与读取游标。</summary>
        public bool Begin()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                return false;
            }

            // 麦克风会话持续运行：仅首次启动，不再每次重建 clip（避免 End→Start 循环导致的第二次录音失败）
            if (_clip == null)
            {
                _deviceName = ResolveMicDeviceName();
                _clip = Microphone.Start(_deviceName, true, LoopSeconds, CaptureSampleRate);
                if (_clip == null)
                {
                    return false;
                }
                _ringSize = 0;
            }

            _samples.Clear();
            _lastReadPos = SafeGetPosition();
            _capturing = true;
            return true;
        }

        /// <summary>
        /// 录音期间每帧调用：把自上次读取以来的新样本以小段追加到内部缓冲。
        /// 位置回绕（cur &lt; _lastReadPos）时读取"尾段 + 头段"补齐新音频，尾段终点使用
        /// 自适应实测边界，不读陈旧/未写入区段。不处于录音态时为无操作。
        /// </summary>
        public void Poll()
        {
            if (!_capturing || _clip == null || _clip.samples <= 0)
            {
                return;
            }

            int total = _clip.samples;
            int cur = ClampPosition(SafeGetPosition(), total);
            if (cur == _lastReadPos)
            {
                return;
            }

            if (cur > _lastReadPos)
            {
                ReadInto(_lastReadPos, cur);
                // 正常推进时顺带修正回绕边界估计（观测位置必然不超过真实边界）
                if (_ringSize > 0 && cur > _ringSize)
                {
                    _ringSize = cur;
                }
            }
            else
            {
                // 真实环形回绕：新音频 = 尾段 [_lastReadPos, _ringSize) + 头段 [0, cur)
                if (_ringSize <= 0 || _ringSize > total)
                {
                    _ringSize = Math.Min(Math.Max(_lastReadPos, cur), total);
                    MiyakoCarryServiceAssistantPlugin.Logger.LogInfo($"麦克风回绕边界估计：{_ringSize} 样本");
                }
                int ringEnd = Math.Min(_ringSize, total);
                ReadInto(_lastReadPos, ringEnd);
                ReadInto(0, cur);
            }
            _lastReadPos = cur;
        }

        /// <summary>
        /// 丢弃当前累积但保持麦克风会话：清空缓冲并把游标重置到当前位置。
        /// 用于 FreeTalk 首次检测到语音时去掉待机空白，让录音从语音开始。
        /// </summary>
        public void Reset()
        {
            _samples.Clear();
            _lastReadPos = SafeGetPosition();
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

        /// <summary>把 clip 区间 [from, to) 的样本读入累积缓冲。</summary>
        private void ReadInto(int from, int to)
        {
            int count = to - from;
            if (count <= 0)
            {
                return;
            }
            var chunk = new float[count];
            try
            {
                _clip.GetData(chunk, from);
            }
            catch
            {
                return;
            }
            _samples.AddRange(chunk);
        }

        /// <summary>结束本段录音：做最后一次轮询后返回本段全部样本（不停止麦克风）。</summary>
        public float[] End()
        {
            if (!_capturing)
            {
                return Array.Empty<float>();
            }
            Poll();
            _capturing = false;

            if (_samples.Count == 0)
            {
                return Array.Empty<float>();
            }
            var data = _samples.ToArray();
            _samples.Clear();
            return data;
        }

        /// <summary>丢弃当前段录音（仅重置游标与缓冲，麦克风保持运行）。用于门控关闭等无需提取的场景。</summary>
        public void Abort()
        {
            _capturing = false;
            _samples.Clear();
            _lastReadPos = SafeGetPosition();
        }

        /// <summary>终止麦克风会话（生命周期结束时调用）。</summary>
        public void Stop()
        {
            _capturing = false;
            _samples.Clear();
            if (_clip != null)
            {
                try { Microphone.End(_deviceName); } catch { }
                UnityEngine.Object.Destroy(_clip);
                _clip = null;
            }
        }

        private int SafeGetPosition()
        {
            try
            {
                return Math.Max(0, Microphone.GetPosition(_deviceName));
            }
            catch
            {
                return 0;
            }
        }

        private static int ClampPosition(int value, int total)
        {
            if (value < 0)
            {
                return 0;
            }
            if (value >= total)
            {
                return total - 1;
            }
            return value;
        }

        public bool IsCapturing => _capturing;

        /// <summary>当前麦克风写位置（使用与录音一致的缓存设备名，供 FreeTalk VAD 窗口采样）。</summary>
        public int CurrentPosition => SafeGetPosition();

        /// <summary>实际采样率：以 Microphone 返回的 clip 频率为准（设备可能强制 48kHz），保证 WAV 头与数据一致。</summary>
        public int SampleRate => _clip != null && _clip.frequency > 0 ? _clip.frequency : CaptureSampleRate;
        public int Channels => CaptureChannels;

        /// <summary>FreeTalk 模式下 VAD 窗口采样需要的活动 AudioClip。</summary>
        public AudioClip ActiveClip => _clip;
    }
}
