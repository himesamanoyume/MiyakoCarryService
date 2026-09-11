using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 设定护航的攻击部位，并叠加弹道预测的提前量
    /// </summary>
    public sealed class GetPartToShootPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.GetPartToShoot));

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static bool Prefix(EnemyInfo __instance, ref Vector3 __result)
        {
            if (!McsMgr.IsMcsBotPlayer(__instance.Owner.ProfileId))
            {
                return true;
            }

            var mcsBotPlayerData = __instance.Owner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                return true;
            }

            __result = GetPartToShootPos(__instance, mcsBotPlayerData);
            return false;
        }

        private static Vector3 GetPartToShootPos(EnemyInfo enemyInfo, McsBotPlayerData mcsBotPlayerData)
        {
            if (enemyInfo.Owner.WeaponManager.UnderbarrelLauncherController.IsActive)
            {
                return enemyInfo.CurrPosition;
            }

            if (!enemyInfo.HaveSeenPersonal || Time.time - enemyInfo.FirstTimeSeen <= enemyInfo.Owner.Settings.FileSettings.Aiming.ANY_PART_SHOOT_TIME)
            {
                var enemyPart = enemyInfo._allParts[mcsBotPlayerData.AimingBodyPartType];
                if (enemyPart.CanShoot && enemyInfo._allPartsVision[mcsBotPlayerData.AimingBodyPartType].Visible)
                {
                    enemyInfo.LastPartToShoot = enemyPart;
                }
                else
                {
                    return ApplyLead(enemyInfo, mcsBotPlayerData, enemyInfo.GetVisiblePartToShoot());
                }
            }

            if (enemyInfo.LastPartToShoot == null)
            {
                return enemyInfo.CurrPosition + Vector3.up;
            }

            return ApplyLead(enemyInfo, mcsBotPlayerData, enemyInfo.LastPartToShoot.GetPartPositionWithOffset());
        }

        private static Vector3 ApplyLead(EnemyInfo enemyInfo, McsBotPlayerData mcsBotPlayerData, Vector3 basePos)
        {
            if (basePos == Vector3.zero)
            {
                return basePos;
            }

            var weapon = enemyInfo.Owner.WeaponManager?.CurrentWeapon;
            if (weapon == null)
            {
                return basePos;
            }

            var ammoTemplate = weapon.CurrentAmmoTemplate;
            if (ammoTemplate == null)
            {
                return basePos;
            }

            var muzzleVelocity = ammoTemplate.InitialSpeed * weapon.SpeedFactor;
            if (muzzleVelocity <= 0f)
            {
                return basePos;
            }

            var firePort = enemyInfo.Owner.WeaponRoot.position;
            var distance = new Vector3(basePos.x - firePort.x, 0f, basePos.z - firePort.z).magnitude;
            var confidence = BotSettingUtils.GetPredictConfidence(distance, mcsBotPlayerData.CarryServiceLevel);
            if (confidence <= 0f)
            {
                return basePos;
            }

            var controlVelocity = enemyInfo.Person.Velocity;
            var estimatedVelocity = mcsBotPlayerData.GetEstimatedEnemyVelocity(enemyInfo);
            var targetVelocity = controlVelocity.magnitude >= 0.5f || estimatedVelocity.magnitude < 1f ? controlVelocity : estimatedVelocity;
            var bulletHasGravity = weapon.IsGrenadeLauncher;

            return Tools.GetPredictedAimPoint(firePort, basePos, targetVelocity, ammoTemplate, muzzleVelocity, bulletHasGravity, confidence);
        }
    }
}