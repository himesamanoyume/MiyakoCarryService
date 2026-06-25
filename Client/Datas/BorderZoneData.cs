
using System;
using System.Linq;
using EFT.Interactive;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class BorderZoneData : TriggerData
    {
        private WeakReference<BorderZone> _borderZoneRef;
        public BorderZone BorderZone => _borderZoneRef.TryGetTarget(out var borderZone) ? borderZone : null;

        public BorderZoneData(BorderZone borderZone) : base()
        {
            _borderZoneRef = new WeakReference<BorderZone>(borderZone);
            _colliders = borderZone.GetComponentsInChildren<Collider>().ToList();
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
    }
}