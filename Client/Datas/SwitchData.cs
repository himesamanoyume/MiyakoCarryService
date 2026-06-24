using System;
using EFT.Interactive;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class SwitchData : InteractiveObjectData
    {
        private WeakReference<Switch> _switchRef;
        public Switch Switch => _switchRef.TryGetTarget(out var @switch) ? @switch : null;

        public SwitchData(Switch @switch): base()
        {
            _switchRef = new WeakReference<Switch>(@switch);
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

        public override bool IsDisabled() => Switch.DoorState is not EDoorState.Shut;

        public override bool IsProxyActionAllowed() => true;
    }
}