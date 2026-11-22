using System;
using System.Reflection;

namespace MiyakoCarryService.Client.Utils
{
    internal static class Il2CppExceptionSafety
    {
        private static Func<IntPtr, string> _ourHook;
        private static Func<IntPtr, string> _previousHook;
        private static MethodInfo _defaultBuildMessage;
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            try
            {
                _previousHook = Il2CppException.ParseMessageHook;
                _defaultBuildMessage = typeof(Il2CppException).GetMethod("BuildMessage", BindingFlags.Static | BindingFlags.NonPublic);

                _ourHook = FormatMessage;
                Il2CppException.ParseMessageHook = _ourHook;
                _installed = true;

                McsLogger.LogInfo($"[Il2CppExceptionSafety] Installed (previousHook={(_previousHook == null ? "none" : "kept")}, defaultFormatter={(_defaultBuildMessage == null ? "missing" : "found")})");
            }
            catch (Exception e)
            {
                McsLogger.LogError($"[Il2CppExceptionSafety.Install] {e}");
            }
        }

        private static string FormatMessage(IntPtr exception)
        {
            long address = exception.ToInt64();
            if (address < 0x10000 || (address & 0x7) != 0)
            {
                return $"<IL2CPP exception pointer 0x{address:X} is not a valid object; message formatting skipped>";
            }

            try
            {
                if (_previousHook != null)
                {
                    return _previousHook(exception);
                }

                if (_defaultBuildMessage == null)
                {
                    return $"<IL2CPP exception 0x{address:X}>";
                }

                Il2CppException.ParseMessageHook = null;
                try
                {
                    return _defaultBuildMessage.Invoke(null, [exception]) as string ?? $"<IL2CPP exception 0x{address:X}>";
                }
                finally
                {
                    Il2CppException.ParseMessageHook = _ourHook;
                }
            }
            catch (Exception e)
            {
                return $"<IL2CPP exception 0x{address:X}; formatting failed: {e.Message}>";
            }
        }
    }
}
