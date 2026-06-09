using System;
using EFT.Interactive;
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

        protected override Transform GetTransfrom() => TransitPoint.transform;

        public override void Dispose()
        {
            base.Dispose();
            _transitRef = null;
        }
    }
}