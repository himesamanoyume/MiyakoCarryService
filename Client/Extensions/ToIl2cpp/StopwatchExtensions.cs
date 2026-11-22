using System.Diagnostics;

namespace MiyakoCarryService.Client.Extensions.ToIl2Cpp
{
    public static class StopwatchExtensions
    {
        extension(Stopwatch stopwatch)
        {
            public Il2CppSystem.Diagnostics.Stopwatch ToIl2Cpp()
            {
                var il2CppStopwatch = new Il2CppSystem.Diagnostics.Stopwatch();
                if (stopwatch == null)
                {
                    return il2CppStopwatch;
                }

                il2CppStopwatch.elapsed = stopwatch.ElapsedTicks;
                if (stopwatch.IsRunning)
                {
                    il2CppStopwatch.started = Stopwatch.GetTimestamp();
                }

                return il2CppStopwatch;
            }
        }
    }
}