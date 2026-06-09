using System;
using EFT.Interactive;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class ExfilData : TriggerData
    {
        private WeakReference<ExfiltrationPoint> _exfilRef;
        public ExfiltrationPoint ExfiltrationPoint => _exfilRef.TryGetTarget(out var exfil) ? exfil : null;
        public bool HasUnmetRequirements = false;

        public ExfilData(ExfiltrationPoint exfiltrationPoint) : base()
        {
            _exfilRef = new WeakReference<ExfiltrationPoint>(exfiltrationPoint);
        }

        protected override Transform GetTransfrom() => ExfiltrationPoint.transform;

        public override void Dispose()
        {
            base.Dispose();
            _exfilRef = null;
        }
    }
}