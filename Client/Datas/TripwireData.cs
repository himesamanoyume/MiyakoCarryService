
using System;
using System.Collections.Generic;
using EFT.SynchronizableObjects;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class TripwireData : ObstacleData
    {
        private WeakReference<TripwireSynchronizableObject> _tripwireRef;
        public TripwireSynchronizableObject Tripwire => _tripwireRef.TryGetTarget(out var tripwire) ? tripwire : null;

        public TripwireData(TripwireSynchronizableObject tripwire) : base()
        {
            _tripwireRef = new WeakReference<TripwireSynchronizableObject>(tripwire);
            _colliders = new List<Collider>()
            {
                CreateBoxCollider(tripwire.FromPosition, tripwire.ToPosition)
            };
            InitObstacle();
        }

        private BoxCollider CreateBoxCollider(Vector3 fromPosition, Vector3 toPosition)
        {
            var dir = toPosition - fromPosition;
            dir.y = 0f;
            var magnitude = dir.magnitude;
            var forward = (magnitude > 0.0001f) ? (dir / magnitude) : Vector3.forward;

            var length = Mathf.Max(0.3f, magnitude + 0.3f);
            var center = (fromPosition + toPosition) * 0.5f;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            var size = new Vector3(0.2f, 1.8f, length);

            var gameObject = new GameObject("BoxCollider");
            gameObject.transform.position = center;
            gameObject.transform.rotation = rotation;

            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = size;
            return box;
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
            _tripwireRef = null;
        }
    }
}