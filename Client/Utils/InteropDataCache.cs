using System;
using System.Collections.Generic;

namespace MiyakoCarryService.Client.Utils
{
    public static class InteropDataCache<TValue> where TValue : class
    {
        private static readonly Dictionary<IntPtr, TValue> _cache = new();
        private static readonly HashSet<IntPtr> _negatives = new();
        private static readonly Dictionary<(IntPtr, string, string), TValue> _compoundCache = new();
        private static readonly HashSet<(IntPtr, string, string)> _compoundNegatives = new();

        public static bool TryGetValue(IntPtr pointer, out TValue value)
        {
            if (pointer == IntPtr.Zero)
            {
                value = null;
                return false;
            }

            return _cache.TryGetValue(pointer, out value) && value != null;
        }

        public static void Set(IntPtr pointer, TValue value)
        {
            if (pointer == IntPtr.Zero || value == null)
            {
                return;
            }

            _negatives.Remove(pointer);
            _cache[pointer] = value;
        }

        public static TValue GetOrCreate(IntPtr pointer, Func<TValue> factory, bool cacheNegative = true)
        {
            if (pointer == IntPtr.Zero)
            {
                return null;
            }

            if (_cache.TryGetValue(pointer, out var cached))
            {
                return cached;
            }

            if (_negatives.Contains(pointer))
            {
                return null;
            }

            var value = factory();
            if (value != null)
            {
                _cache[pointer] = value;
            }
            else if (cacheNegative)
            {
                _negatives.Add(pointer);
            }
            return value;
        }

        public static bool TryGetValue(IntPtr pointer, string key1, string key2, out TValue value)
        {
            if (pointer == IntPtr.Zero)
            {
                value = null;
                return false;
            }

            return _compoundCache.TryGetValue((pointer, key1, key2), out value) && value != null;
        }

        public static void Set(IntPtr pointer, string key1, string key2, TValue value)
        {
            if (pointer == IntPtr.Zero || value == null)
            {
                return;
            }

            _compoundNegatives.Remove((pointer, key1, key2));
            _compoundCache[(pointer, key1, key2)] = value;
        }

        public static TValue GetOrCreate(IntPtr pointer, string key1, string key2, Func<TValue> factory, bool cacheNegative = true)
        {
            if (pointer == IntPtr.Zero)
            {
                return null;
            }

            var key = (pointer, key1, key2);
            if (_compoundCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            if (_compoundNegatives.Contains(key))
            {
                return null;
            }

            var value = factory();
            if (value != null)
            {
                _compoundCache[key] = value;
            }
            else if (cacheNegative)
            {
                _compoundNegatives.Add(key);
            }
            return value;
        }

        public static void Clear()
        {
            _cache.Clear();
            _negatives.Clear();
            _compoundCache.Clear();
            _compoundNegatives.Clear();
        }
    }
}
