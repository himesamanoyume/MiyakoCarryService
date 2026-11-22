using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 监测玩家对护航的误伤情况，并视情况触发对应事件
    /// </summary>
    public sealed class ApplyDamagePatch : ModulePatch
    {
        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.ApplyDamage));

#if DEBUG
        [PatchPrefix]
        public static void Prefix(ActiveHealthController __instance, EBodyPart bodyPart, ref float damage, DamageInfo damageInfo)
        {
            var player = __instance.Player;

            if (player == null || player.IsYourPlayer)
            {
                return;
            }

            if (!McsMgr.IsMcsBotPlayer(player.ProfileId))
            {
                return;
            }

            if (MiyakoCarryServicePlugin.McsBotPlayerNoDamage.Value)
            {
                damage = 0;
                return;
            }

            if (MiyakoCarryServicePlugin.McsBotPlayerKeepAlive.Value)
            {
                var healthController = player.ActiveHealthController;
                var currentHealth = healthController.GetBodyPartHealth(bodyPart, false);
                var maxAllowedDamage = Math.Max(0f, currentHealth.Current - 1f);  
                damage = Math.Min(damage, maxAllowedDamage);  
                return;
            }
        }
#endif

        [PatchPostfix]
        public static void Postfix(ActiveHealthController __instance, EBodyPart bodyPart, float damage, DamageInfo damageInfo)
        {
            var player = __instance.Player;

            if (player == null)
            {
                return;
            }

            if (!Tools.IsHost)
            {
                return;
            }

            var attacker = damageInfo.Player?.iPlayer;
            // 护航 lead 被打时：记录攻击者并让其小队转向。
            // 原先由 GameLoop.SpawnMcsBotPlayer 里的 add_BeingHitAction 订阅实现；5.0 的 Il2CppInterop
            // 无法把参数含非 blittable 结构体 DamageInfo 的托管 lambda 转成原生委托，故改由本补丁承接。
            // 5.0：iPlayer 的声明类型是接口 IPlayer，托管 `is Player` 恒为 false → TryCast
            var attackerPlayer = attacker == null ? null : attacker.TryCast<Player>();

            if (attackerPlayer != null && McsMgr.IsMcsLeadPlayer(player.ProfileId) && !McsMgr.IsMcsBotPlayer(attackerPlayer.ProfileId))
            {
                MarkLeadAttackerFor(player, attackerPlayer);
            }
            if (attackerPlayer != null && McsMgr.IsMcsBotPlayer(player.ProfileId) && !McsMgr.IsMcsBotPlayer(attackerPlayer.ProfileId))
            {
                var mcsBotPlayerData = player.AIData.BotOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.LastHitTime = Time.time;
                    mcsBotPlayerData.LastHitShooter = Tools.IsForbiddenEnemy(attackerPlayer) ? null : attackerPlayer;
                    mcsBotPlayerData.AddSuppression(damage);
                }

                // 护航被打：把攻击者加入本队敌人队列（何时开火交给护航 AI 自己决定，不强制 GoalEnemy）。
                if (!Tools.IsForbiddenEnemy(attackerPlayer))
                {
                    var escortGroup = player.BotsGroup;
                    if (escortGroup != null && !escortGroup.IsEnemy(attackerPlayer) && Tools.TryMakeRoomForEnemy(escortGroup))
                    {
                        escortGroup.AddEnemy(attackerPlayer);
                    }
                }
            }

            // 老板打中中立 AI（俄军/平民）：把该目标加入护航组敌人队列（何时开火交给护航 AI）。
            if (attackerPlayer != null && damage > 0
                && McsMgr.IsMcsLeadPlayer(attackerPlayer.ProfileId)
                && player.AIData?.BotOwner != null
                && player.Profile?.Info?.Settings != null
                && Tools.IsNeutralBot(player.Profile.Info.Settings.Role))
            {
                var leadGroup = attackerPlayer.BotsGroup;
                if (leadGroup != null && !leadGroup.IsEnemy(player) && Tools.TryMakeRoomForEnemy(leadGroup))
                {
                    leadGroup.AddEnemy(player);
                }
            }

            if (damage <= 0)
            {
                return;
            }

            if (attacker == null)
            {
                return;
            }

            var isMcsBotAttacker = McsMgr.IsMcsBotPlayer(attacker.ProfileId);
            var isMcsLeadAttacker = McsMgr.IsMcsLeadPlayer(attacker.ProfileId);
            var isMcsBotInjuredPlayer = McsMgr.IsMcsBotPlayer(player.ProfileId);
            var isMcsLeadInjuredPlayer = McsMgr.IsMcsLeadPlayer(player.ProfileId);

            var commonHp = __instance.GetBodyPartHealth(EBodyPart.Common);
            var headHp = __instance.GetBodyPartHealth(EBodyPart.Head);
            var chestHp = __instance.GetBodyPartHealth(EBodyPart.Chest);
            var isDead = commonHp.AtMinimum || headHp.AtMinimum || chestHp.AtMinimum;

            if (isDead)
            {
                if (isMcsBotInjuredPlayer)
                {
                    var mcsLeadPlayer = player.AIData.BotOwner.GetMcsBotPlayerData().LeadPlayer;
                    var mcsBotPlayer = McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(mcsLeadPlayer.ProfileId).FirstOrDefault();
                    if (mcsBotPlayer != null)
                    {
                        mcsBotPlayer.BotOwner.TalkMsg(mcsLeadPlayer, mcsBotPlayer.BotOwner.GetPlayer, new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.OnFriendlyDown
                        });
                    }
                }
                else if (isMcsBotAttacker && !isMcsLeadInjuredPlayer)
                {
                    var mcsBotPlayerBotOwner = attacker.AIData.BotOwner;
                    if (mcsBotPlayerBotOwner != null)
                    {
                        var mcsLeadPlayer = mcsBotPlayerBotOwner.GetMcsBotPlayerData().LeadPlayer;
                        mcsBotPlayerBotOwner.TalkMsg(mcsLeadPlayer, mcsBotPlayerBotOwner.GetPlayer, new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.EnemyDown
                        });
                    }
                }
            }

            // var notMcsLeaderButIsFikaPlayer = attacker.Profile.Info.GroupId == "Fika";
            var notMcsLeaderButIsFikaPlayer = false;
            if (isMcsBotAttacker && isMcsLeadInjuredPlayer)
            {
                if (!isDead)
                {
                    return;
                }
                McsMgr.SendCompensation(player.ProfileId);
            }
            else if (isMcsBotInjuredPlayer && !isMcsBotAttacker && (notMcsLeaderButIsFikaPlayer || isMcsLeadAttacker))
            {
                McsMgr.AddPunish(attacker.ProfileId, isDead ? 0.1560d : 0.0107d, isDead, notMcsLeaderButIsFikaPlayer);
            }
        }

        /// <summary>
        /// 护航 lead 被打时：标记攻击者，并让该 lead 的小队转向攻击者。
        /// 逻辑逐字复刻自原先 GameLoop.SpawnMcsBotPlayer 里的 add_BeingHitAction 订阅。
        /// </summary>
        private static void MarkLeadAttackerFor(Player leadPlayer, Player attackerPlayer)
        {
            if (attackerPlayer == null || !attackerPlayer.HealthController.IsAlive)
            {
                return;
            }

            if (leadPlayer.BotsGroup == null)
            {
                return;
            }

            var mcsBotPlayers = McsMgr.GetAllMcsSquadMembersByMcsLeadId(leadPlayer.ProfileId);
            var mcsBotPlayer = mcsBotPlayers?.FirstOrDefault();
            if (mcsBotPlayer?.AIData?.BotOwner?.BotFollower?.BossToFollow is not McsAILeadPlayer mcsAILeadPlayer)
            {
                return;
            }

            var attacker = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(attackerPlayer.ProfileId);
            if (attacker == null)
            {
                return;
            }

            mcsAILeadPlayer.MarkLeadAttacker(attacker);

            if (Time.time - mcsAILeadPlayer.LastLeadReportTime < 0.25f)
            {
                return;
            }

            mcsAILeadPlayer.LastLeadReportTime = Time.time;
            mcsAILeadPlayer.CalcGoalEnemy(attacker);
        }
    }
}
