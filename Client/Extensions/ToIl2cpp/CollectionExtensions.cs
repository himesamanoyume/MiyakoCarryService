using System.Collections.Generic;
using UnityEngine;

namespace MiyakoCarryService.Client.Extensions.ToIl2Cpp
{
    public static class CollectionExtensions
    {
        extension<T>(ICollection<T> source)
        {
            public T McsRandom()
            {
                var array = new T[source.Count];
                source.CopyTo(array, 0);
                return array[Random.Range(0, array.Length)];
            }
        }
    }
}