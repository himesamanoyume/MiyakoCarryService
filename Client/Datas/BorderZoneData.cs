
using System;
using System.Linq;
using EFT.Interactive;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class BorderZoneData : ObstacleData
    {
        private BorderZone _borderZoneRef;
        public BorderZone BorderZone => _borderZoneRef;

        public BorderZoneData(BorderZone borderZone) : base()
        {
            _borderZoneRef = borderZone;
            _colliders = borderZone.GetComponentsInChildren<Collider>().ToList();
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
            _borderZoneRef = null;
        }
    }
}