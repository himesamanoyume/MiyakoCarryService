using System;
using System.Collections.Generic;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Navigation
{
    // NavBridge 诊断日志：Info 级别写入 LogOutput.log，按 key 限频（每秒一条），排障期默认开启
    public static class NavBridgeDebug
    {
        private static readonly Dictionary<string, float> NextLogTime = new Dictionary<string, float>();

        public static bool Enabled = true;

        public static void Log(string key, string message, float interval = 1f)
        {
            if (!Enabled)
            {
                return;
            }

            var time = Time.time;
            if (NextLogTime.TryGetValue(key, out var next) && time < next)
            {
                return;
            }
            NextLogTime[key] = time + interval;

            // 带墙钟时间：脚本引擎热重载后日志连成一片，靠时间戳区分轮次
            McsLogger.LogInfo($"[NavBridge][{key}] [{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
    }
}
