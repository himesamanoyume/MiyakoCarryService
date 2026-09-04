using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

                segments.Add(NormalizeFrame(method));
            }

            if (segments.Count == 0)
            {
                segments.Add((typeof(McsLogger), nameof(Log)));
            }

            var logSiteSegment = segments[0];
            var callerSegments = segments.Skip(1).ToList();

            var orderedCallers = callerSegments.AsEnumerable().Reverse().Distinct().ToList();

            var chain = BuildChain(orderedCallers, logSiteSegment);

            var result = $"[{chain}]\n{data}";
            _logger.Log(level, result);
        }

        private static string BuildChain(List<(Type DeclaringType, string MethodName)> orderedCallers, (Type DeclaringType, string MethodName) logSiteSegment)
        {
            var chainSegments = new List<string>(orderedCallers.Count + 1);

            foreach (var callerSegment in orderedCallers)
            {
                chainSegments.Add($"{callerSegment.DeclaringType.Name}.{callerSegment.MethodName}()");
            }

            var logSiteTypeName = logSiteSegment.DeclaringType.FullName ?? logSiteSegment.DeclaringType.Name;
            chainSegments.Add($"{logSiteTypeName}.{logSiteSegment.MethodName}()");

            return string.Join(" -> ", chainSegments);
        }

        private static bool IsCompilerGeneratedFrame(MethodBase method)
        {
            if (method.Name.IndexOf('<') >= 0)
            {
                return true;
            }

            var declaringType = method.DeclaringType;
            if (declaringType != null && declaringType.Name.IndexOf('<') >= 0)
            {
                return true;
            }

            return false;
        }

        private static (Type DeclaringType, string MethodName) NormalizeFrame(MethodBase method)
        {
            var displayType = method.DeclaringType;

            while (displayType != null && displayType.Name.IndexOf('<') >= 0)
            {
                displayType = displayType.DeclaringType;
            }

            if (!IsCompilerGeneratedFrame(method))
            {
                return (displayType ?? method.DeclaringType, method.Name);
            }

            var displayMethod = ExtractHostMethodName(method);
            return (displayType ?? method.DeclaringType, displayMethod);
        }

        private static string ExtractHostMethodName(MethodBase method)
        {
            var hostMethodName = ExtractAngleToken(method.Name);
            if (hostMethodName != null)
            {
                return hostMethodName;
            }

            if (method.DeclaringType != null)
            {
                hostMethodName = ExtractAngleToken(method.DeclaringType.Name);
                if (hostMethodName != null)
                {
                    return hostMethodName;
                }
            }

            return method.Name;
        }

        private static string ExtractAngleToken(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var start = name.IndexOf('<');
            if (start < 0)
            {
                return null;
            }

            var end = name.IndexOf('>', start + 1);
            if (end < 0)
            {
                return null;
            }

            var token = name.Substring(start + 1, end - start - 1);
            return string.IsNullOrEmpty(token) ? null : token;
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