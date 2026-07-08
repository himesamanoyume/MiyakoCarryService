using System;
using EFT.Interactive;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Datas
{
    public class DoorData : InteractableObjectData
    {
        private WeakReference<Door> _doorRef;
        public Door Door => _doorRef.TryGetTarget(out var door) ? door : null;

        public DoorData(Door door): base()
        {
            _doorRef = new WeakReference<Door>(door);
        }

        public override string GetActionName()
        {
            throw new NotImplementedException();
        }

        public override string GetActionTargetName(Vector3 myPlayerPos)
        {
            throw new NotImplementedException();
        }

        public override bool IsDisabled()
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            base.Dispose();
            _doorRef = null;
        }

        public override Vector3 GetPos()
        {
            var center = Door.transform.position;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var samplePos = new Vector3(
                    center.x + MyExtensions.Random(-1, 1),
                    center.y + MyExtensions.Random(-1, 1),
                    center.z + MyExtensions.Random(-1, 1)
                );

                if (NavMesh.SamplePosition(samplePos, out var hit, 1f, -1))
                {
                    return hit.position;
                }
            }
            return center;
        }

        public override string Id() => Door.Id;

        public override bool IsProxyActionDisabled() => true;

        public override WorldInteractiveObject GetWorldInteractiveObject() => Door;
    }
}