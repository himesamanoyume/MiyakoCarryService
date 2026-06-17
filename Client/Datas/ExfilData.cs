using System;
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Datas
{
    public class ExfilData : TriggerData
    {
        private WeakReference<ExfiltrationPoint> _exfilRef;
        public ExfiltrationPoint ExfiltrationPoint => _exfilRef.TryGetTarget(out var exfil) ? exfil : null;
        public bool HasUnmetRequirements = false;
        private Collider _collider;

        public ExfilData(ExfiltrationPoint exfiltrationPoint) : base()
        {
            _exfilRef = new WeakReference<ExfiltrationPoint>(exfiltrationPoint);
            _collider = exfiltrationPoint.transform.GetComponent<Collider>();
        }

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

                if (NavMesh.SamplePosition(samplePos, out var hit, 2f, -1))
                {
                    return hit.position;
                }
            }
            return _collider.transform.position;
        }
        public override string GetActionName() => ExfiltrationPoint.Settings.Name.McsLocalized();
        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), Mathf.RoundToInt(Vector3.Distance(myPlayerPos, ExfiltrationPoint.gameObject.transform.position)));
        public override bool IsDisabled() => ExfiltrationPoint.Status switch
        {
            EExfiltrationStatus.NotPresent => true,
            _ => false
        };

        public override void Dispose()
        {
            base.Dispose();
            _exfilRef = null;
        }
    }
}