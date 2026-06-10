using System;
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class TransitData : TriggerData
    {
        private WeakReference<TransitPoint> _transitRef;
        public TransitPoint TransitPoint => _transitRef.TryGetTarget(out var transit) ? transit : null;

        public TransitData(TransitPoint transitPoint) : base()
        {
            _transitRef = new WeakReference<TransitPoint>(transitPoint);
        }

        public override Transform GetTransfrom() => TransitPoint.transform;
        public override string GetActionName() => TransitPoint.parameters.description.McsLocalized();
        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format("距离我 {0} M", Mathf.RoundToInt(Vector3.Distance(myPlayerPos, TransitPoint.transform.position)));
        public override bool IsDisabled() => !TransitPoint.IsActive;

        public override void Dispose()
        {
            base.Dispose();
            _transitRef = null;
        }

    }
}