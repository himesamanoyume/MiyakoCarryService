using System;
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using Systems.Effects;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 借鉴friendlyPmc
    /// </summary>
    public sealed class PlayHitEffectPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EffectsCommutator), nameof(EffectsCommutator.PlayHitEffect));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(EffectsCommutator __instance, EftBulletClass info, ShotInfoClass playerHitInfo)
        {
            var shooter = info.Player;
            if (shooter == null)
            {
                return;
            }

            if (McsMgr.IsMcsLeadPlayer(shooter.iPlayer.ProfileId) || McsMgr.IsMcsBotPlayer(shooter.iPlayer.ProfileId))
            {
                return;
            }

            if (!__instance.IsHitPointAlreadyProcessed(info.HitPoint))
            {
                foreach (var botOwner in McsMgr.GetAllAliveMcsBotPlayer())
                {
                    if (botOwner.Position.McsSqrDistance(info.HitPoint) <= botOwner.Settings.FileSettings.Mind.BULLET_FEEL_CLOSE_SDIST * botOwner.Settings.FileSettings.Mind.BULLET_FEEL_CLOSE_SDIST)
                    {
                        botOwner.BotsGroup.ReportAboutEnemy(shooter.iPlayer, EEnemyPartVisibleType.Visible, botOwner);
                    }
                }
            }
        }
    }
}