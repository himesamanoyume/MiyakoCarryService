using System;
using EFT.Interactive;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class DoorData : InteractiveObjectData
    {
        private WeakReference<Door> _doorRef;
        public Door Door => _doorRef.TryGetTarget(out var door) ? door : null;

        public DoorData(Door door): base()
        {
            _doorRef = new WeakReference<Door>(door);
        }

        public override void ExcuteProxyAction()
        {
            throw new NotImplementedException();
        }

        public override string GetActionName()
        {
            throw new NotImplementedException();
        }

        public override string GetActionTargetName(Vector3 myPlayerPos)
        {
            throw new NotImplementedException();
        }

        public override bool IsDisabled() => false;

        public override bool IsProxyActionAllowed() => true;
    }
}