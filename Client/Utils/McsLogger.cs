using System;
using System.Diagnostics;
using System.Reflection;
using BepInEx.Logging;

namespace MiyakoCarryService.Client.Utils
{
    /// <summary>
    /// 借鉴SAIN日志
    /// </summary>
    public static class McsLogger
    {
        private static readonly ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource("MiyakoCarryService");

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
            var methodsString = string.Empty;
            Type declaringType = null;

            var stackTrace = new StackTrace(2);
            var max = GetMaxFrames(level);
            max = Math.Min(max, stackTrace.FrameCount);

            for (int i = 0; i < max; i++)
            {
                MethodBase method = stackTrace.GetFrame(i)?.GetMethod();
                if (method == null)
                {
                    continue;
                }

                if (method.DeclaringType == typeof(McsLogger))
                {
                    continue;
                }

                if (declaringType == null)
                {
                    declaringType = method.DeclaringType;
                }

                if (!string.IsNullOrEmpty(methodsString))
                {
                    methodsString = "." + methodsString;
                }

                methodsString = $"{method.Name}()" + methodsString;
            }

            if (declaringType == null)
            {
                declaringType = typeof(McsLogger);
            }

            var result = $"[{level}] [{declaringType.FullName ?? declaringType.Name}] : [{methodsString}] : [{data}]";
            _logger.Log(level, result);
        }

        private static int GetMaxFrames(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    return 1;
                case LogLevel.Warning:
                    return 2;
                case LogLevel.Error:
                    return 3;
                case LogLevel.Fatal:
                    return 4;
                default:
                    return 1;
            }
        }
    }
}
