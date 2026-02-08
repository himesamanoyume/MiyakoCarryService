using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;
using Systems.Effects;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 借鉴friendlyPmc
    /// </summary>
    internal sealed class PlayHitEffectPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EffectsCommutator), nameof(EffectsCommutator.PlayHitEffect));

        private static SquadMgr SquadMgr
        { 
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPostfix]
        public static void Postfix(EffectsCommutator __instance, EftBulletClass info, ShotInfoClass playerHitInfo)
        {
            var shooter = info.Player;
            if (shooter == null)
            {
                return;
            }

            if (SquadMgr.IsMcsLeadPlayer(shooter.iPlayer.ProfileId) || SquadMgr.IsMcsBotPlayer(shooter.iPlayer.ProfileId))
            {
                return;
            }

            if (!__instance.IsHitPointAlreadyProcessed(info.HitPoint))
            {
                foreach (var botOwner in SquadMgr.GetAllAliveMcsBotPlayer())
                {
                    if ((botOwner.Position - info.HitPoint).sqrMagnitude <= botOwner.Settings.FileSettings.Mind.BULLET_FEEL_CLOSE_SDIST)
                    {
                        botOwner.BotsGroup.ReportAboutEnemy(shooter.iPlayer, EEnemyPartVisibleType.Sence, botOwner);
                    }
                }
            }
        }
    }
}