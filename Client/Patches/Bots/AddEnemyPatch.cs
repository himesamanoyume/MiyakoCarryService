using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 避免护航Bot将护航老板当做敌人，同时让护航Bot同步敌人信息，并且面对友好Bot类型时不主动开火
    /// </summary>
    public sealed class AddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotsGroup), nameof(BotsGroup.AddEnemy));

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        [ThreadStatic]
        private static bool _isPropagating;

        [PatchPrefix]
        public static bool Prefix(BotsGroup __instance, IPlayer person, EBotEnemyCause cause, ref bool __result)
        {
            if (person == null)
            {
                return true;
            }

            if (__instance._defWildSpawnType is WildSpawnType.shooterBTR or WildSpawnType.bossZryachiy or WildSpawnType.followerZryachiy)
            {
                if (McsMgr.IsMcsBotPlayer(person.ProfileId))
                {
                    __result = false;
                    return false;
                }
            }

            foreach (var botOwner in __instance._members)
            {
                if (McsMgr.IsMcsBotPlayer(botOwner.ProfileId))
                {
                    if (Tools.IsForbiddenEnemy(person))
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            return true;
        }

        [PatchPostfix]
        public static void Postfix(BotsGroup __instance, IPlayer person, EBotEnemyCause cause)
        {
            if (person == null || _isPropagating)
            {
                return;
            }

            if (McsMgr.IsMcsLeadPlayer(person.ProfileId) || McsMgr.IsMcsBotPlayer(person.ProfileId))
            {
                MirrorHostilityToSquadGroups(__instance, person);
                return;
            }

            if (person.Profile.Info.GroupId is "Mcs" or "Fika" || McsMgr.IsMcsBotPlayer(person.ProfileId) || McsMgr.IsMcsLeadPlayer(person.ProfileId))
            {
                return;
            }

            string mcsLeadPlayerId = null;
            foreach (var member in __instance._members)
            {
                if (McsMgr.IsMcsBotPlayer(member.ProfileId))
                {
                    mcsLeadPlayerId = McsMgr.GetMcsLeadPlayerByMcsBotPlayerId(member.ProfileId)?.GetPlayer?.ProfileId;
                    if (mcsLeadPlayerId != null)
                    {
                        break;
                    }
                }
            }

            if (mcsLeadPlayerId == null)
            {
                return;
            }

            var allMcsMembers = McsMgr.GetAllMcsSquadMembersByMcsLeadId(mcsLeadPlayerId);

            _isPropagating = true;
            try
            {
                foreach (var member in allMcsMembers)
                {
                    var botGroup = member.AIData?.BotOwner?.BotsGroup;
                    if (botGroup == null || botGroup == __instance)
                    {
                        continue;
                    }

                    var attackerGroup = person.AIData?.BotOwner?.BotsGroup;
                    if (attackerGroup != null)
                    {
                        foreach (var enemy in attackerGroup._members)
                        {
                            botGroup.AddEnemy(enemy.GetPlayer, EBotEnemyCause.callForHelp1);
                        }
                    }
                    else
                    {
                        botGroup.AddEnemy(person, EBotEnemyCause.callForHelp1);
                    }
                }
            }
            finally
            {
                _isPropagating = false;
            }
        }

        private static void MirrorHostilityToSquadGroups(BotsGroup hostileGroup, IPlayer mcsPerson)
        {
            var mcsLeadPlayerId = McsMgr.IsMcsLeadPlayer(mcsPerson.ProfileId)
                ? mcsPerson.ProfileId
                : McsMgr.GetMcsLeadPlayerByMcsBotPlayerId(mcsPerson.ProfileId)?.ProfileId;
            if (mcsLeadPlayerId == null)
            {
                return;
            }

            var mcsAILeadPlayer = McsMgr.GetMcsAILeadPlayerByMcsLeadPlayerId(mcsLeadPlayerId);
            if (mcsAILeadPlayer?.McsLeadPlayer == null)
            {
                return;
            }

            _isPropagating = true;
            try
            {
                var leadGroup = mcsAILeadPlayer.McsLeadPlayer.BotsGroup;
                if (leadGroup != null)
                {
                    AddGroupMembersAsEnemy(leadGroup, hostileGroup);
                }

                var mcsBotPlayers = McsMgr.GetAllMcsSquadMembersByMcsLeadId(mcsLeadPlayerId);
                foreach (var mcsBotPlayer in mcsBotPlayers)
                {
                    var botGroup = mcsBotPlayer?.AIData?.BotOwner?.BotsGroup;
                    if (botGroup == null || botGroup == leadGroup)
                    {
                        continue;
                    }

                    AddGroupMembersAsEnemy(botGroup, hostileGroup);
                }
            }
            finally
            {
                _isPropagating = false;
            }
        }

        private static void AddGroupMembersAsEnemy(BotsGroup targetGroup, BotsGroup hostileGroup)
        {
            foreach (var hostileMember in hostileGroup._members)
            {
                if (hostileMember == null || hostileMember.IsDead)
                {
                    continue;
                }

                targetGroup.AddEnemy(hostileMember.GetPlayer, EBotEnemyCause.callForHelp1);
            }
        }
    }
}
