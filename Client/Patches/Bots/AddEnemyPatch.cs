using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 避免护航Bot将护航老板当做敌人，同时让护航Bot同步敌人信息，并且面对友好Bot类型时不主动开火
    /// </summary>
    public sealed class AddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotGroupHostility), nameof(BotGroupHostility.AddEnemyAction));

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        [ThreadStatic]
        private static bool _isPropagating;

        [ThreadStatic]
        private static bool _blockedThisCall;

        [PatchPrefix]
        public static bool Prefix(BotGroupHostility __instance, IPlayer player)
        {
            _blockedThisCall = false;

            if (player == null)
            {
                return true;
            }

            var group = __instance.OwnedGroup;
            if (group == null)
            {
                return true;
            }

            if (group._defWildSpawnType is WildSpawnType.shooterBTR or WildSpawnType.bossZryachiy or WildSpawnType.followerZryachiy)
            {
                if (McsMgr.IsMcsBotPlayer(player.ProfileId))
                {
                    _blockedThisCall = true;
                    return false;
                }
            }

            if (!GroupHasMcsMember(group))
            {
                return true;
            }

            if (Tools.IsForbiddenEnemy(player))
            {
                _blockedThisCall = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判定一个 BotsGroup 是否属于 MCS 护航：组 boss 或任一成员是 MCS 护航 bot。
        /// ⚠️ 必须同时看 <c>_initialBot</c>：加敌可能发生在成员尚未挂上（或成员表为空）时，
        /// 只看 <c>_members</c> 会漏拦。
        /// </summary>
        private static bool GroupHasMcsMember(BotsGroup group)
        {
            if (group == null)
            {
                return false;
            }

            if (group._initialBot != null && McsMgr.IsMcsBotPlayer(group._initialBot.ProfileId))
            {
                return true;
            }

            foreach (var member in group._members)
            {
                if (member != null && McsMgr.IsMcsBotPlayer(member.ProfileId))
                {
                    return true;
                }
            }

            return false;
        }

        [PatchPostfix]
        public static void Postfix(BotGroupHostility __instance, IPlayer player)
        {
            var blocked = _blockedThisCall;
            _blockedThisCall = false;

            if (player == null || _isPropagating || blocked)
            {
                return;
            }

            var __instanceGroup = __instance.OwnedGroup;
            if (__instanceGroup == null)
            {
                return;
            }

            if (McsMgr.IsMcsLeadPlayer(player.ProfileId) || McsMgr.IsMcsBotPlayer(player.ProfileId))
            {
                MirrorHostilityToSquadGroups(__instanceGroup, player);
                return;
            }

            if (player.Profile.Info.GroupId is "Mcs" || McsMgr.IsMcsBotPlayer(player.ProfileId) || McsMgr.IsMcsLeadPlayer(player.ProfileId))
            {
                return;
            }

            string mcsLeadPlayerId = null;
            foreach (var member in __instanceGroup._members)
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
                    if (botGroup == null || botGroup == __instanceGroup)
                    {
                        continue;
                    }

                    var attackerGroup = player.AIData?.BotOwner?.BotsGroup;
                    if (attackerGroup != null)
                    {
                        foreach (var enemy in attackerGroup._members)
                        {
                            if (!Tools.TryMakeRoomForEnemy(botGroup))
                            {
                                continue;
                            }

                            botGroup.AddEnemy(enemy.GetPlayer);
                        }
                    }
                    else
                    {
                        if (!Tools.TryMakeRoomForEnemy(botGroup))
                        {
                            continue;
                        }

                        botGroup.AddEnemy(player);
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

                if (!Tools.TryMakeRoomForEnemy(targetGroup))
                {
                    return;
                }

                targetGroup.AddEnemy(hostileMember.GetPlayer);
            }
        }
    }
}
