
using UnityEngine;

namespace MiyakoCarryService.Client.Extensions
{
    public static class Vector3Extensions
    {
        extension(Vector3 a)
        {
            public float McsSqrDistance(Vector3 b)
            {
                var dx = a.x - b.x;
                var dy = a.y - b.y;
                var dz = a.z - b.z;
                return dx * dx + dy * dy + dz * dz;
            }
        }
    }
}
