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
    /// 设定护航的攻击部位
    /// </summary>
    public sealed class GetPartToShootPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.GetPartToShoot));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

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

            if (!mcsBotPlayerData.IsMcsLayerActive)
            {
                return true;
            }

            var visibleType = __instance.VisibleType;
            if (visibleType - EEnemyPartVisibleType.GreenSence <= 1)
            {
                __result = __instance.method_7();
                return false;
            }
            if (visibleType == EEnemyPartVisibleType.Visible)
            {
                __result = GetPartToShootPos(__instance, mcsBotPlayerData);
                return false;
            }
            if (!__instance.Owner.WeaponManager.UnderbarrelLauncherController.IsActive)
            {
                __result = __instance.GetBodyPartPosition();
                return false;
            }
            __result = __instance.CurrPosition;
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
                var enemyPart = enemyInfo.AllParts[mcsBotPlayerData.AimingBodyPartType];
                if (enemyPart.CanShoot && enemyInfo.AllPartsVision[mcsBotPlayerData.AimingBodyPartType].Visible)
                {
                    enemyInfo.LastPartToShoot = enemyPart;
                }
                else
                {
                    return enemyInfo.method_8();
                }
            }
            if (enemyInfo.LastPartToShoot == null)
            {
                return Vector3.zero;
            }
            return enemyInfo.LastPartToShoot.GetPartPositionWithOffset();
        }
    }
}