using DrakiaXYZ.BigBrain.Brains;
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class ShootFromStationaryLogic : McsBotBaseLogic
    {
        private ShootFromStationaryBaseLogic _baseLogic;

        private float _scanPhase;
        private const float ScanPeriod = 4f; 
        private const float ScanDistance = 30f;
        private const float ScanPitchDown = 2f;

        public ShootFromStationaryLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            var stationary = BotOwner.WeaponManager.Stationary;

            _baseLogic.UpdateNodeByMain(data);
            if (BotOwner.WeaponManager.Stationary.IsEnemyAtSector(BotOwner.WeaponManager.Stationary.CurLink))
            {
                return;
            }

            ScanSector(stationary.CurLink);
        }

        private void ScanSector(StationaryWeaponLink link)
        {
            var weapon = link.Weapon;
            if (weapon == null)
            {
                return;
            }

            var halfAngleDeg = Mathf.Acos(Mathf.Clamp(link.CosAngleBase, -1f, 1f)) * Mathf.Rad2Deg;

            _scanPhase += Time.deltaTime / ScanPeriod;
            var tri = Mathf.PingPong(_scanPhase, 1f);
            var yawDeg = Mathf.Lerp(-halfAngleDeg, halfAngleDeg, tri);

            var baseDir = link.InitialDir;
            baseDir.y = 0f;
            if (baseDir.sqrMagnitude < 0.001f)
            {
                return;
            }
            baseDir.Normalize();

            var dir = Quaternion.AngleAxis(yawDeg, Vector3.up) * baseDir;
            dir = Quaternion.AngleAxis(ScanPitchDown, Vector3.Cross(dir, Vector3.up)) * dir;
            var scanPoint = weapon.OperatorPosition + dir * ScanDistance;
            BotOwner.AimingManager.CurrentAiming.SetTarget(scanPoint);
            BotOwner.AimingManager.NodeUpdate();
        }
    }
}