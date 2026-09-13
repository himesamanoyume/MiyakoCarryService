using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    public static class NavBridgeDebug
    {
        private static readonly Dictionary<string, float> _nextLogTime = new Dictionary<string, float>();

        public static bool Enabled = true;

        public static void Log(string key, string message, float interval = 1f)
        {
            if (!Enabled)
            {
                return;
            }

            var time = Time.time;
            if (_nextLogTime.TryGetValue(key, out var next) && time < next)
            {
                return;
            }
            _nextLogTime[key] = time + interval;

            McsLogger.LogInfo($"[NavBridge][{key}] [{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
    }
}
