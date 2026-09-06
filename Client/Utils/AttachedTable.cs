
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MiyakoCarryService.Client.Utils
{
    public sealed class AttachedTable<TKey, TValue> where TKey : class where TValue : class
    {
        private sealed class Entry
        {
            public TValue Value;
        }

        private static readonly Entry _empty = new();
        private readonly ConditionalWeakTable<TKey, Entry> _table = new();

        public TValue GetOrCreate(TKey key, Func<TKey, TValue> factory)
        {
            return GetOrCreate(key, factory, true);
        }

        public TValue GetOrCreate(TKey key, Func<TKey, TValue> factory, bool cacheNegative)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (cacheNegative)
            {
                var entry = _table.GetValue(key, k =>
                {
                    var value = factory(k);
                    return value != null ? new Entry { Value = value } : _empty;
                });
                return entry.Value;
            }
            else
            {
                if (_table.TryGetValue(key, out var entry))
                {
                    if (entry.Value != null)
                    {
                        return entry.Value;
                    }

                    _table.Remove(key);
                }

                var _value = factory(key);
                if (_value != null)
                {
                    _table.AddOrUpdate(key, new Entry { Value = _value });
                }
                return _value;
            }
        }

        public bool TryGet(TKey key, out TValue value)
        {
            value = null;
            if (key == null)
            {
                return false;
            }

            if (_table.TryGetValue(key, out var entry))
            {
                value = entry.Value;
                return value != null;
            }
            return false;
        }

        public void Set(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _table.AddOrUpdate(key, value != null ? new Entry { Value = value } : _empty);
        }

        public bool Remove(TKey key)
        {
            if (key == null)
            {
                return false;
            }
            return _table.Remove(key);
        }

        public void Clear()
        {
            _table.Clear();
        }

        public int Count
        {
            get
            {
                var count = 0;
                foreach (var _ in _table)
                {
                    count++;
                }
                return count;
            }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> Enumerate()
        {
            foreach (var pair in _table)
            {
                if (pair.Value != null && pair.Value.Value != null)
                {
                    yield return new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Value);
                }
            }
        }
    }
}