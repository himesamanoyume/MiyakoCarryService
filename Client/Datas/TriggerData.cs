
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class TriggerData : WorldData
    {
        protected Collider _collider;
        public override Vector3 GetPos()
        {
            var bounds = _collider.bounds;
            var center = bounds.center;
            var extents = bounds.extents;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                var samplePos = new Vector3(
                    center.x + GClass856.Random(-extents.x, extents.x),
                    center.y + GClass856.Random(-extents.y, extents.y),
                    center.z + GClass856.Random(-extents.z, extents.z)
                );

                if (NavMesh.SamplePosition(samplePos, out var hit, 1f, -1))
                {
                    return hit.position;
                }
            }
            return _collider.transform.position;
        }
    }
}