using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;

namespace MiyakoCarryService.Client.Utils
{
    internal static class InteropRegistry<T> where T : Il2CppObjectBase
    {
        private static readonly Dictionary<IntPtr, T> _items = new();
        private static readonly List<IntPtr> _deadKeys = new();

        public static int Count => _items.Count;

        public static void Add(T item)
        {
            if (item == null)
            {
                return;
            }

            var pointer = item.Pointer;
            if (pointer == IntPtr.Zero)
            {
                return;
            }

            _items[pointer] = item;
        }

        public static void Remove(T item)
        {
            if (item == null)
            {
                return;
            }

            _items.Remove(item.Pointer);
        }

        public static void CopyTo(List<T> buffer)
        {
            buffer.Clear();
            if (_items.Count == 0)
            {
                return;
            }

            foreach (var pair in _items)
            {
                var value = pair.Value;
                if (value == null || value.WasCollected)
                {
                    _deadKeys.Add(pair.Key);
                    continue;
                }

                buffer.Add(value);
            }

            foreach (var deadKey in _deadKeys)
            {
                _items.Remove(deadKey);
            }

            _deadKeys.Clear();
        }

        public static void Clear()
        {
            _items.Clear();
            _deadKeys.Clear();
        }
    }
}
