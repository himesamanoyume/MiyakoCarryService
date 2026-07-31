using System;
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Datas
{
    public class StationaryWeaponData : InteractableObjectData
    {
        private WeakReference<StationaryWeapon> _stationaryWeaponRef;
        public StationaryWeapon StationaryWeapon => _stationaryWeaponRef.TryGetTarget(out var stationaryWeapon) ? stationaryWeapon : null;
        private WeakReference<StationaryWeaponLink> _stationaryWeaponLinkRef;
        public StationaryWeaponLink StationaryWeaponLink => _stationaryWeaponLinkRef.TryGetTarget(out var stationaryWeaponLink) ? stationaryWeaponLink : null;

        public StationaryWeaponData(StationaryWeapon stationaryWeapon) : base()
        {
            _stationaryWeaponRef = new WeakReference<StationaryWeapon>(stationaryWeapon);
            if (!Tools.IsHost)
            {
                return;
            }

            var weapon = StationaryWeapon;
            if (weapon == null)
            {
                return;
            }

            var link = CreateStationaryWeaponLink(weapon);
            if (link != null)
            {
                _stationaryWeaponLinkRef = new WeakReference<StationaryWeaponLink>(link);
            }
        }

        public override string GetActionName() => StationaryWeapon.Item.LocalizedName();

        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), Mathf.RoundToInt(Vector3.Distance(myPlayerPos, StationaryWeapon.transform.position)));

        public override bool IsDisabled() => false;

        public override void Dispose()
        {
            base.Dispose();
            _stationaryWeaponRef = null;
        }

        public override Vector3 GetPos()
        {
            var center = StationaryWeapon.transform.position;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var samplePos = new Vector3(
                    center.x + GClass856.Random(-1, 1),
                    center.y + GClass856.Random(-1, 1),
                    center.z + GClass856.Random(-1, 1)
                );

                if (NavMesh.SamplePosition(samplePos, out var hit, 1f, -1))
                {
                    return hit.position;
                }
            }
            return center;
        }

        public override string Id() => StationaryWeapon.Id;

        public override bool IsProxyActionDisabled() => StationaryWeapon.Locked;

        public override InteractableObject GetInteractiveObject() => StationaryWeapon;

        private StationaryWeaponLink CreateStationaryWeaponLink(StationaryWeapon weapon)
        {
            var newLink = new GameObject($"McsStationaryWeaponLink_{weapon.Id}");
            newLink.transform.position = weapon.OperatorPosition;

            var link = newLink.GetOrAddComponent<StationaryWeaponLink>();

            if (!link.Init() || link.BadLoad || link.Weapon != weapon)
            {
                UnityEngine.Object.Destroy(newLink);
                return null;
            }

            var corePoint = AICorePointHolder.GetClosest(weapon.OperatorPosition);
            if (corePoint != null)
            {
                link.SetCorePoint(corePoint);
            }
            link.Init();

            return link;
        }
    }
}