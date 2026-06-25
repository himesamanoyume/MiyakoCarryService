
using System;
using System.Linq;
using EFT.GameTriggers;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class TriggerZoneData : ObstacleData
    {
        private WeakReference<TriggerZone> _triggerZoneRef;
        public TriggerZone TriggerZone => _triggerZoneRef.TryGetTarget(out var triggerZone) ? triggerZone : null;
        public string Id;

        public TriggerZoneData(TriggerZone triggerZone, string triggerZoneId) : base()
        {
            _triggerZoneRef = new WeakReference<TriggerZone>(triggerZone);
            _colliders = triggerZone.GetComponentsInChildren<Collider>().ToList();
            Id = triggerZoneId;
            InitObstacle();
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
            _triggerZoneRef = null;
        }
    }
}