using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 老板开火报点：老板开火时沿弹道方向推断交战目标（头/身距弹道线足够近的敌）并全队报点，
    /// 让护航即时响应老板的交战意图。不进威胁上下文（开火是意图而非被威胁），走标准报点弱化仲裁——
    /// 无目标护航立即接管，有目标护航按 25% 距离弱化决定是否转向（老板意图不打断护航自身威胁应对）
    /// </summary>
    public sealed class MakingShotPatch : ModulePatch
    {
        /// <summary>
        /// 弹道推断最大搜索距离（米）：沿弹道超过此距离的目标不参与推断
        /// </summary>
        private const float SHOT_SCAN_MAX_DISTANCE = 300f;

        /// <summary>
        /// 弹道垂直判定基准允许距离（米）：距枪口越远允许越宽（散射与瞄准误差）
        /// </summary>
        private const float SHOT_PERPENDICULAR_BASE = 0.65f;

        /// <summary>
        /// 弹道垂直允许距离随距离增长率（米/米）
        /// </summary>
        private const float SHOT_PERPENDICULAR_GROWTH = 0.012f;

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.OnMakingShot));

        [PatchPostfix]
        public static void Postfix(Player __instance)
        {
            try
            {
                if (__instance == null || !__instance.HealthController.IsAlive || __instance.IsAI)
                {
                    return;
                }

                // 只处理 MCS 老板玩家的开火（老板必为真人玩家，IsAI 已前置短路全场 bot 的开火）
                if (!McsMgr.IsMcsLeadPlayer(__instance.ProfileId))
                {
                    return;
                }

                var mcsAILeadPlayer = McsMgr.GetMcsAILeadPlayerByMcsLeadPlayerId(__instance.ProfileId);
                if (mcsAILeadPlayer == null)
                {
                    return;
                }

                // 节流：全自动连发时每发都做弹道推断无意义（无论推断成败都刷新，避免打空方向每发重算）
                var time = Time.time;
                if (time - mcsAILeadPlayer.LastLeadShotReportTime < McsAILeadPlayer.LEAD_SHOT_REPORT_INTERVAL)
                {
                    return;
                }
                mcsAILeadPlayer.LastLeadShotReportTime = time;

                var shotOrigin = __instance.Position + Vector3.up * 1.6f;
                var shotDirection = __instance.LookDirection;

                var engagementTarget = FindShotEngagementTarget(__instance, shotOrigin, shotDirection);
                if (engagementTarget == null)
                {
                    return;
                }

                mcsAILeadPlayer.CalcGoalEnemy(engagementTarget);
            }
            catch (Exception e)
            {
                McsLogger.LogError(e);
            }
        }

        /// <summary>
        /// 弹道推断交战目标：沿老板开火方向搜索头/身距弹道线足够近的敌（已敌对者，防止朝中立目标
        /// 方向开火时误报升级敌对），取综合评分最优（弹道垂直距离优先，同分近者优先）
        /// </summary>
        private static Player FindShotEngagementTarget(Player leadPlayer, Vector3 shotOrigin, Vector3 shotDirection)
        {
            Player bestTarget = null;
            var bestScore = float.MaxValue;

            foreach (var target in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                if (target == null || !target.HealthController.IsAlive || !target.IsAI)
                {
                    continue;
                }

                if (McsMgr.IsMcsBotPlayer(target.ProfileId) || McsMgr.IsMcsLeadPlayer(target.ProfileId))
                {
                    continue;
                }

                // 只对已敌对目标推断：老板朝中立单位方向开火（打的是身后的敌）不误报
                if (leadPlayer.BotsGroup == null || !leadPlayer.BotsGroup.IsEnemy(target))
                {
                    continue;
                }

                // 头/身两个采样点，取弹道垂直距离更近者
                var headOffset = target.Position + Vector3.up * 1.6f - shotOrigin;
                var bodyOffset = target.Position + Vector3.up * 0.8f - shotOrigin;

                foreach (var targetOffset in new[] { headOffset, bodyOffset })
                {
                    var alongRay = Vector3.Dot(targetOffset, shotDirection);
                    if (alongRay <= 0f || alongRay > SHOT_SCAN_MAX_DISTANCE)
                    {
                        continue;
                    }

                    var perpendicularDistance = (targetOffset - shotDirection * alongRay).magnitude;
                    var allowedDistance = Mathf.Clamp(SHOT_PERPENDICULAR_BASE + alongRay * SHOT_PERPENDICULAR_GROWTH, 0.8f, 2f);
                    if (perpendicularDistance > allowedDistance)
                    {
                        continue;
                    }

                    var score = perpendicularDistance * perpendicularDistance + alongRay * 0.0001f;
                    if (score < bestScore)
                    {
                        bestTarget = target;
                        bestScore = score;
                    }
                }
            }

            return bestTarget;
        }
    }
}
