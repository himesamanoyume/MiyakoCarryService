using System.Reflection;
using EFT;
using EFT.Ballistics;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;
using Il2CppSystems.Effects;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 借鉴friendlyPmc
    /// </summary>
    public sealed class PlayHitEffectPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EffectsCommutator), nameof(EffectsCommutator.PlayHitEffect));

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(EffectsCommutator __instance, Shot info, PlayerHitInfo playerHitInfo)
        {
            if (!Tools.IsHost)
            {
                return;
            }
            
            var shooter = info.Player;
            if (shooter == null)
            {
                return;
            }

            // if (shooter.iPlayer.Profile.Info.GroupId is "Fika" or "Mcs" || McsMgr.IsMcsLeadPlayer(shooter.iPlayer.ProfileId) || McsMgr.IsMcsBotPlayer(shooter.iPlayer.ProfileId))
            if (shooter.iPlayer.Profile.Info.GroupId is "Mcs" || McsMgr.IsMcsLeadPlayer(shooter.iPlayer.ProfileId) || McsMgr.IsMcsBotPlayer(shooter.iPlayer.ProfileId))
            {
                return;
            }

            if (!__instance.IsHitPointAlreadyProcessed(info.HitPoint))
            {
                // 中立角色（俄军/平民）在附近开枪不构成对我们的敌对：只做近失压制，不主动加敌。
                // 若其真的打中我们/lead，由受击流程（ApplyDamagePatch）建立敌对。
                var isNeutralShooter = shooter.iPlayer?.Profile?.Info?.Settings != null
                    && Tools.IsNeutralBot(shooter.iPlayer.Profile.Info.Settings.Role);

                foreach (var mcsBotPlayer in McsMgr.GetAllAliveMcsBotPlayer())
                {
                    var botOwner = mcsBotPlayer.AIData.BotOwner;
                    if (mcsBotPlayer.Position.McsSqrDistance(info.HitPoint) <= botOwner.Settings.FileSettings.Mind.BULLET_FEEL_CLOSE_SDIST * botOwner.Settings.FileSettings.Mind.BULLET_FEEL_CLOSE_SDIST)
                    {
                        var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                        if (mcsBotPlayerData != null)
                        {
                            mcsBotPlayerData.LastShotAtTime = Time.time;
                            mcsBotPlayerData.AddSuppressionFromNearMiss();
                        }

                        if (isNeutralShooter)
                        {
                            continue;
                        }

                        if (!Tools.TryMakeRoomForEnemy(mcsBotPlayer.BotsGroup))
                        {
                            continue;
                        }

                        mcsBotPlayer.BotsGroup.AddEnemy(shooter.iPlayer);
                        var mcsLeadPlayer = McsMgr.GetMcsLeadPlayerByMcsBotPlayerId(mcsBotPlayer.ProfileId);
                        var mcsAILeadPlayer = McsMgr.GetMcsAILeadPlayerByMcsLeadPlayerId(mcsLeadPlayer.ProfileId);
                        // 5.0：iPlayer 的声明类型是接口 IPlayer，托管 `is Player` 恒为 false → TryCast
                        var shooterPlayer = shooter.iPlayer == null ? null : shooter.iPlayer.TryCast<Player>();
                        if (shooterPlayer != null)
                        {
                            mcsAILeadPlayer.CalcGoalEnemy(shooterPlayer);
                        }
                    }
                }
            }
        }
    }
}