using System;
using System.Collections;
using MiyakoCarryService.Client.Utils;
namespace MiyakoCarryService.Client.Extensions
{
    public static class CoroutineExtensions
    {
        public static IEnumerator McsSafe(this IEnumerator inner)
        {
            if (inner == null)
            {
                yield break;
            }

            while (true)
            {
                object current = null;
                var hasCurrent = false;
                var finished = false;

                try
                {
                    finished = !inner.MoveNext();
                    if (!finished)
                    {
                        current = inner.Current;
                        hasCurrent = true;
                    }
                }
                catch (Exception ex)
                {
                    McsLogger.LogError($"[CoroutineExtensions.McsSafe] {ex}");
                    finished = true;
                }

                if (finished)
                {
                    yield break;
                }

                if (hasCurrent)
                {
                    yield return current;
                }
            }
        }
    }
}
