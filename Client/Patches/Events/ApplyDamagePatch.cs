using System.Linq;
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 监测玩家对护航的误伤情况
    /// </summary>
    public sealed class ApplyDamagePatch : ModulePatch
    {
        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.ApplyDamage));

        [PatchPostfix]
        public static void Postfix(ActiveHealthController __instance, Player ___Player, EBodyPart bodyPart, float damage, DamageInfoStruct damageInfo)
        {
            if (___Player == null)
            {
                return;
            }

            if (!McsMgr.IsHost)
            {
                return;
            }

            if (damage <= 0)
            {
                return;
            }

            var attacker = damageInfo.Player?.iPlayer;

            if (attacker == null)
            {
                return;
            }

            var isMcsBotAttacker = McsMgr.IsMcsBotPlayer(attacker.ProfileId);
            var isMcsLeadAttacker = McsMgr.IsMcsLeadPlayer(attacker.ProfileId);
            var isMcsBotInjuredPlayer = McsMgr.IsMcsBotPlayer(___Player.ProfileId);
            var isMcsLeadInjuredPlayer = McsMgr.IsMcsLeadPlayer(___Player.ProfileId);

            var commonHp = __instance.GetBodyPartHealth(EBodyPart.Common);
            var headHp = __instance.GetBodyPartHealth(EBodyPart.Head);
            var chestHp = __instance.GetBodyPartHealth(EBodyPart.Chest);
            var isDead = commonHp.AtMinimum || headHp.AtMinimum || chestHp.AtMinimum;

            if (isDead)
            {
                if (isMcsBotInjuredPlayer)
                {
                    var mcsLeadPlayer = ___Player.AIData.BotOwner.GetMcsBotPlayerData().LeadPlayer;
                    var mcsBotPlayerBotOwner = McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(mcsLeadPlayer.ProfileId).FirstOrDefault();
                    if (mcsBotPlayerBotOwner != null)
                    {
                        EventMgr.Notify(new SubtitlesMgrHandleFikaEvent
                        {
                            McsLeadPlayerId = mcsLeadPlayer.ProfileId,
                            McsBotPlayerId = mcsBotPlayerBotOwner.ProfileId,
                            Msg = new McsMsg
                            {
                                PhraseTrigger = EPhraseTrigger.OnFriendlyDown
                            }
                        });
                    }
                }
                else if (isMcsBotAttacker && !isMcsLeadInjuredPlayer)
                {
                    EventMgr.Notify(new SubtitlesMgrHandleFikaEvent
                    {
                        McsLeadPlayerId = attacker.AIData.BotOwner.GetMcsBotPlayerData().LeadPlayer.ProfileId,
                        McsBotPlayerId = attacker.ProfileId,
                        Msg = new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.EnemyDown
                        }
                    });
                }
                else if (isMcsLeadInjuredPlayer)
                {
                    var mcsLeadPlayer = ___Player.AIData.BotOwner.GetMcsBotPlayerData().LeadPlayer;
                    var mcsBotPlayerBotOwner = McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(mcsLeadPlayer.ProfileId).FirstOrDefault();
                    if (mcsBotPlayerBotOwner != null)
                    {
                        EventMgr.Notify(new OnMcsLeadPlayerDownEvent
                        {
                            McsLeadPlayerId = mcsLeadPlayer.ProfileId,
                            McsBotPlayerId = mcsBotPlayerBotOwner.ProfileId
                        });
                    }
                }
            }

            if (isMcsBotAttacker && isMcsLeadInjuredPlayer)
            {
                if (!isDead)
                {
                    return;
                }
                McsMgr.SendCompensation(___Player.ProfileId);
            }
            else if (isMcsBotInjuredPlayer && !isMcsBotAttacker && (notMcsLeaderButIsFikaPlayer || isMcsLeadAttacker))
            {
                McsMgr.AddPunish(attacker.ProfileId, isDead ? 0.1560d : 0.0107d, isDead, notMcsLeaderButIsFikaPlayer);
            }
        }
    }
}