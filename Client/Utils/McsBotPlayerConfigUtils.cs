using System;
using System.Collections.Generic;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Client.Utils
{
    internal static class McsBotPlayerConfigUtils
    {
        private class Field
        {
            public Func<McsConfigValue> GetLocal;
            public Action<McsConfigValue> Apply;
        }

        private static readonly Dictionary<string, Field> _fields = new();

        public static void Register<T>(string key, Func<T> getLocal, Action<T> apply)
        {
            _fields[key] = new Field
            {
                GetLocal = () => Encode(getLocal()),
                Apply = v => apply(Decode<T>(v))
            };
        }

        public static Dictionary<string, McsConfigValue> Snapshot()
        {
            var dict = new Dictionary<string, McsConfigValue>();
            foreach (var kvp in _fields)
            {
                dict[kvp.Key] = kvp.Value.GetLocal();
            }
            return dict;
        }

        public static void ApplyAll(McsBotPlayerConfig config)
        {
            if (config?.Extensions == null) return;
            foreach (var kvp in config.Extensions)
            {
                if (_fields.TryGetValue(kvp.Key, out var field))
                {
                    field.Apply(kvp.Value);
                }
            }
        }

        public static McsConfigValue Encode<T>(T value)
        {
            return value switch
            {
                bool b => new McsConfigValue
                    {
                        Type = EMcsConfigValueType.Bool,
                        BoolValue = b
                    },
                int i => new McsConfigValue
                    {
                        Type = EMcsConfigValueType.Int,
                        IntValue = i
                    },
                long l => new McsConfigValue
                    {
                        Type = EMcsConfigValueType.Long,
                        IntValue = l
                    },
                float f => new McsConfigValue
                    {
                        Type = EMcsConfigValueType.Float,
                        FloatValue = f
                    },
                string s => new McsConfigValue
                    {
                        Type = EMcsConfigValueType.String,
                        StringValue = s ?? ""
                    },
                Enum e => new McsConfigValue
                    {
                        Type = EMcsConfigValueType.Enum,
                        IntValue = Convert.ToInt64(e)
                    },
                _ => throw new NotSupportedException()
                
            };
        }

        public static T Decode<T>(McsConfigValue v)
        {
            var t = typeof(T);

            if (t == typeof(bool))
            {
                return (T)(object)v.BoolValue;
            }

            if (t == typeof(int))
            {
                return (T)(object)(int)v.IntValue;
            }

            if (t == typeof(long))
            {
                return (T)(object)v.IntValue;
            }

            if (t == typeof(float))
            {
                return (T)(object)v.FloatValue;
            }

            if (t == typeof(string))
            {
                return (T)(object)(v.StringValue ?? "");
            }

            if (t.IsEnum)
            {
                return (T)Enum.ToObject(t, v.IntValue);
            }

            throw new NotSupportedException();
        }
    }
}