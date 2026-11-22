
using System;
using System.Linq;
using CommonAssets.Scripts.Game.LabyrinthEvent;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class BarbedWireData : ObstacleData
    {
        private TrapSyncable _barbedWireRef;
        public TrapSyncable BarbedWire => _barbedWireRef;

        public BarbedWireData(TrapSyncable barbedWire) : base()
        {
            _barbedWireRef = barbedWire;
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