using System;
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Datas
{
    public class TransitData : TriggerData
    {
        private WeakReference<TransitPoint> _transitRef;
        public TransitPoint TransitPoint => _transitRef.TryGetTarget(out var transit) ? transit : null;
        private Collider _collider;

        public TransitData(TransitPoint transitPoint) : base()
        {
            _transitRef = new WeakReference<TransitPoint>(transitPoint);
            _collider = transitPoint.transform.GetComponent<Collider>();
        }

        public override Vector3 GetPos()
        {
            var bounds = _collider.bounds;
            var center = bounds.center;
            var extents = bounds.extents;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                var samplePos = new Vector3(
                    center.x + GClass856.Random(-extents.x, extents.x),
                    center.y + GClass856.Random(-extents.y, extents.y),
                    center.z + GClass856.Random(-extents.z, extents.z)
                );

                if (NavMesh.SamplePosition(samplePos, out var hit, 2f, -1))
                {
                    return hit.position;
                }
            }
            return _collider.transform.position;
        }
        public override string GetActionName() => TransitPoint.parameters.description.McsLocalized();
        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), Mathf.RoundToInt(Vector3.Distance(myPlayerPos, TransitPoint.gameObject.transform.position)));
        public override bool IsDisabled() => !TransitPoint.IsActive;

        public override void Dispose()
        {
            base.Dispose();
            _transitRef = null;
        }

    }
}