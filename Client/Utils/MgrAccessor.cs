using System;
using System.Collections.Generic;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Utils
{
    internal static class MgrAccessor
    {
        private static readonly Dictionary<Type, object> _cache = new();

        public static T Get<T>() where T : IMgr
        {
            var type = typeof(T);
            if (!_cache.TryGetValue(type, out var mgr))
            {
                mgr = GameLoop.Instance.GetMgr<T>();
                _cache[type] = mgr;
            }
            return (T)mgr;
        }
    }
}