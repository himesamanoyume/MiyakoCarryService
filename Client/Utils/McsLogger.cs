using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx.Logging;

namespace MiyakoCarryService.Client.Utils
{
    /// <summary>
    /// 借鉴SAIN日志
    /// </summary>
    public static class McsLogger
    {
        private static readonly ManualLogSource _logger = Logger.CreateLogSource("MiyakoCarryService");

        public static void LogInfo(object data)
        {
            Log(LogLevel.Info, data);
        }

        public static void LogDebug(object data)
        {
            Log(LogLevel.Debug, data);
        }

        public static void LogWarning(object data)
        {
            Log(LogLevel.Warning, data);
        }

        public static void LogError(object data)
        {
            Log(LogLevel.Error, data);
        }

        public static void LogFatal(object data)
        {
            Log(LogLevel.Fatal, data);
        }

        private static void Log(LogLevel level, object data)
        {
            var stackTrace = new StackTrace(2);
            var max = Math.Min(GetMaxFrames(level), stackTrace.FrameCount);

            var segments = new List<(Type DeclaringType, string MethodName)>();
            for (int i = 0; i < max; i++)
            {
                var method = stackTrace.GetFrame(i)?.GetMethod();
                if (method == null || method.DeclaringType == null || method.DeclaringType == typeof(McsLogger))
                {
                    continue;
                }

                segments.Add((method.DeclaringType, method.Name));
            }

            if (segments.Count == 0)
            {
                segments.Add((typeof(McsLogger), nameof(Log)));
            }

            var chain = string.Join(" -> ", segments.Select((segment, index) =>
            {
                var typeName = index == 0 ? segment.DeclaringType.FullName ?? segment.DeclaringType.Name : segment.DeclaringType.Name;
                return $"{typeName}.{segment.MethodName}()";
            }).Reverse());

            var result = $"[{chain}]\n{data}";
            _logger.Log(level, result);
        }

        private static int GetMaxFrames(LogLevel level)
        {
            return level switch
            {
                LogLevel.Warning => 2,
                LogLevel.Error => 3,
                LogLevel.Fatal => 4,
                _ => 1,
            };
        }
    }
}
