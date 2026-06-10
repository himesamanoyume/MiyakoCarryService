using System;
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
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

        public override Transform GetTransfrom() => ExfiltrationPoint.transform;
        public override string GetActionName() => ExfiltrationPoint.Settings.Name.McsLocalized();
        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format("距离我 {0} M", Mathf.RoundToInt(Vector3.Distance(myPlayerPos, ExfiltrationPoint.transform.position)));
        public override bool IsDisabled() => ExfiltrationPoint.Status switch
        {
            EExfiltrationStatus.RegularMode or
            EExfiltrationStatus.Countdown => false,
            _ => true
        };

        public override void Dispose()
        {
            base.Dispose();
            _exfilRef = null;
        }
    }
}