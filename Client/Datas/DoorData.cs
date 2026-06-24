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

        public override void ExcuteProxyAction()
        {
            
        }
        
        public override bool IsProxyActionAllowed() => true;

        public override string Id() => Door.Id;

        public override void Dispose()
        {
            base.Dispose();
            _doorRef = null;
        }
    }
}