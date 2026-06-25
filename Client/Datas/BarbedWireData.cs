
using System;
using System.Linq;
using CommonAssets.Scripts.Game.LabyrinthEvent;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class BarbedWireData : TriggerData
    {
        private WeakReference<TrapSyncable> _barbedWireRef;
        public TrapSyncable BarbedWire => _barbedWireRef.TryGetTarget(out var barbedWire) ? barbedWire : null;

        public BarbedWireData(TrapSyncable barbedWire) : base()
        {
            _barbedWireRef = new WeakReference<TrapSyncable>(barbedWire);
            _colliders = barbedWire.transform.GetChild(0).GetComponentsInChildren<Collider>().ToList();
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