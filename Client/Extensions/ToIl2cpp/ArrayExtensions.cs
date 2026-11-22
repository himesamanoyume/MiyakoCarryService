using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace MiyakoCarryService.Client.Extensions.ToIl2Cpp
{
    public static class ArrayExtensions
    {
        extension<T>(T[] source) where T : class
        {
            public Il2CppReferenceArray<T> ToIl2Cpp()
            {
                return new Il2CppReferenceArray<T>(source ?? Array.Empty<T>());
            }
        }

        extension<T>(T[] source) where T : unmanaged
        {
            public Il2CppStructArray<T> ToIl2CppStructArray()
            {
                return new Il2CppStructArray<T>(source ?? Array.Empty<T>());
            }
        }
    }
}