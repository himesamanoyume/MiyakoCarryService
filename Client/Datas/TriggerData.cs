
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class TriggerData : WorldData
    {
        protected List<Collider> _colliders;
        
        public override Vector3 GetPos()
        {
            if (_colliders.Count > 0)
            {
                var bounds = _colliders.FirstOrDefault().bounds;
                var center = bounds.center;
                var extents = bounds.extents;

                for (int attempt = 0; attempt < 30; attempt++)
                {
                    var samplePos = new Vector3(
                        center.x + MyExtensions.Random(-extents.x, extents.x),
                        center.y + MyExtensions.Random(-extents.y, extents.y),
                        center.z + MyExtensions.Random(-extents.z, extents.z)
                    );

                    if (NavMesh.SamplePosition(samplePos, out var hit, 1f, -1))
                    {
                        return hit.position;
                    }
                }
                return _colliders.FirstOrDefault().transform.position;
            }
            return Vector3.zero;
        }

        public List<Collider> GetColliders()
        {
            return _colliders;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_colliders != null)
            {
                _colliders.Clear();
            }
        }
    }
}