using System;
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Datas
{
    public class SwitchData : InteractableObjectData
    {
        private WeakReference<Switch> _switchRef;
        public Switch Switch => _switchRef.TryGetTarget(out var @switch) ? @switch : null;

        public SwitchData(Switch @switch): base()
        {
            _switchRef = new WeakReference<Switch>(@switch);
        }

        public override string GetActionName() => Classification.ImportantSwitchIdsInfo.TryGetValue(Switch.Id, out var info) ? info.McsLocalized() : Locales.SWITCH.McsLocalized();

        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), Mathf.RoundToInt(Vector3.Distance(myPlayerPos, Switch.transform.position)));

        public override bool IsDisabled() => false;

        public override void Dispose()
        {
            base.Dispose();
            _switchRef = null;
        }

        public override Vector3 GetPos()
        {
            var center = Switch.transform.position;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                if (NavMesh.SamplePosition(center, out var hit, 1f, -1))
                {
                    return hit.position;
                }
            }
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var samplePos = new Vector3(
                    center.x + GClass856.Random(-1, 1),
                    center.y + GClass856.Random(-1, 1),
                    center.z + GClass856.Random(-1, 1)
                );

                if (NavMesh.SamplePosition(samplePos , out var hit, 1f, -1))
                {
                    return hit.position;
                }
            }
            return center;
        }

        public override string Id() => Switch.Id;

        public override bool IsProxyActionDisabled() => Switch.DoorState is not EDoorState.Shut;

        public override WorldInteractiveObject GetWorldInteractiveObject() => Switch;
    }
}