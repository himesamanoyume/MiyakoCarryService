
using System;
using System.Linq;
using EFT.Interactive;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class DamageTriggerData : ObstacleData
    {
        private DamageTrigger _damageTriggerRef;
        public DamageTrigger DamageTrigger => _damageTriggerRef;

        public DamageTriggerData(DamageTrigger damageTrigger) : base()
        {
            _damageTriggerRef = damageTrigger;
            _colliders = damageTrigger.GetComponentsInChildren<Collider>().ToList();
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
            _damageTriggerRef = null;
        }
    }
}