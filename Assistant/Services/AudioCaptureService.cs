using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiyakoCarryService.Assistant.Services
{
    internal sealed class AudioCaptureService
    {
        private AudioClip _clip;
        private const int CaptureSampleRate = 44100;
        private const int CaptureChannels = 1;
        private const int LoopSeconds = 10;
        private const float PreRollSeconds = 0.4f;
        private static readonly int PreRollSamples = (int)(CaptureSampleRate * PreRollSeconds);
        private string _deviceName;
        private int _ringSize;
        private bool _capturing;
        private bool _armed;
        private int _lastReadPos;
        private readonly List<float> _samples = new List<float>();
        private readonly List<float> _preRoll = new List<float>();

        public bool Begin()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                return false;
            }

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
            _preRoll.Clear();
            _armed = false;
            _lastReadPos = SafeGetPosition();
            _capturing = true;
            return true;
        }

        public void Poll()
        {
            if (!_capturing || _clip == null || _clip.samples <= 0)
            {
                return;
            }

            var total = _clip.samples;
            var cur = ClampPosition(SafeGetPosition(), total);
            if (cur == _lastReadPos)
            {
                return;
            }

            if (cur > _lastReadPos)
            {
                Feed(_lastReadPos, cur);
                if (_ringSize > 0 && cur > _ringSize)
                {
                    _ringSize = cur;
                }
            }
            else
            {
                if (_ringSize <= 0 || _ringSize > total)
                {
                    _ringSize = Math.Min(Math.Max(_lastReadPos, cur), total);
                }
                var ringEnd = Math.Min(_ringSize, total);
                Feed(_lastReadPos, ringEnd);
                Feed(0, cur);
            }
            _lastReadPos = cur;
        }

        public void Arm()
        {
            _armed = true;
            _samples.Clear();
            _samples.AddRange(_preRoll);
        }

        public void Reset()
        {
            _armed = false;
            _samples.Clear();
            _preRoll.Clear();
            _lastReadPos = SafeGetPosition();
        }

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

        private void Feed(int from, int to)
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
            if (_armed)
            {
                _samples.AddRange(chunk);
            }
            _preRoll.AddRange(chunk);
            var excess = _preRoll.Count - PreRollSamples;
            if (excess > 0)
            {
                _preRoll.RemoveRange(0, excess);
            }
        }

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

        public void Abort()
        {
            _capturing = false;
            _armed = false;
            _samples.Clear();
            _preRoll.Clear();
            _lastReadPos = SafeGetPosition();
        }

        public void Stop()
        {
            _capturing = false;
            _armed = false;
            _samples.Clear();
            _preRoll.Clear();
            if (_clip != null)
            {
                try
                {
                    Microphone.End(_deviceName);
                }
                catch
                {

                }
                UnityEngine.Object.Destroy(_clip);
                _clip = null;
            }
        }

        public void RestartForNewRaid()
        {
            _capturing = false;
            _armed = false;
            _samples.Clear();
            _preRoll.Clear();
            if (_clip != null)
            {
                try
                {
                    Microphone.End(_deviceName);
                }
                catch
                {

                }
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

        public int CurrentPosition => SafeGetPosition();
        public int SampleRate => _clip != null && _clip.frequency > 0 ? _clip.frequency : CaptureSampleRate;
        public int Channels => CaptureChannels;
        public AudioClip ActiveClip => _clip;
    }
}
