
using System;
using System.Linq;
using CommonAssets.Scripts.Game.LabyrinthEvent;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class BarbedWireData : ObstacleData
    {
        private WeakReference<TrapSyncable> _barbedWireRef;
        public TrapSyncable BarbedWire => _barbedWireRef.TryGetTarget(out var barbedWire) ? barbedWire : null;

        public BarbedWireData(TrapSyncable barbedWire) : base()
        {
            _barbedWireRef = new WeakReference<TrapSyncable>(barbedWire);
            _colliders = barbedWire.transform.GetChild(0).GetComponentsInChildren<Collider>().ToList();
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
            _barbedWireRef = null;
        }
    }
}