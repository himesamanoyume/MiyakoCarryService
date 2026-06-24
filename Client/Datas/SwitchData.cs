using System;
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
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

        public override string GetActionName() => Classification.ImportantSwitchIdsInfo.TryGetValue(Switch.Id, out var info) ? info.McsLocalized() : Locales.SWITCH.McsLocalized();

        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), Mathf.RoundToInt(Vector3.Distance(myPlayerPos, Switch.transform.position)));

        public override bool IsDisabled() => Switch.DoorState is not EDoorState.Shut;

        public override bool IsProxyActionAllowed() => true;

        public override string Id() => Switch.Id;

        public override void Dispose()
        {
            base.Dispose();
            _switchRef = null;
        }
    }
}