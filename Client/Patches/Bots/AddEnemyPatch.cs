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
                    if (person.Profile.Info.GroupId is "Mcs" or "Fika" || McsMgr.IsMcsBotPlayer(person.ProfileId) || McsMgr.IsMcsLeadPlayer(person.ProfileId) || person.Profile.Info.Settings.Role is WildSpawnType.shooterBTR or WildSpawnType.bossZryachiy or WildSpawnType.followerZryachiy)
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

            // 组镜像敌意：任何外部组把老板/护航加为敌人 → 该老板的全部护航组反向把该组全员加为敌
            // （护航对威胁老板的势力有敌对反应，不再"老板被打护航还中立"；__instance 含护航成员时
            // Prefix 已拦截 MCS 系入敌，能走到这里的 __instance 必为外部组）
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

                    // AI 攻击者：镜像传播其所在组全体成员；人类玩家攻击者（无 BotOwner）：仅传播攻击者本身
                    // （统一传 Player 本体做键：原生自然目击流程按 Player 键入 EnemyInfos，BotOwner 键会造成同敌双份记录）
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

        /// <summary>
        /// 组镜像敌意传播：把敌视组（hostileGroup）的全体成员加为该老板的老板组与全部护航组的敌人。
        /// person 为老板时镜像到其 squad；为护航时镜像到其所属老板的 squad（解除"护航被敌视但 squad 无反应"）
        /// </summary>
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
                // 老板组
                var leadGroup = mcsAILeadPlayer.McsLeadPlayer.BotsGroup;
                if (leadGroup != null)
                {
                    AddGroupMembersAsEnemy(leadGroup, hostileGroup);
                }

                // 各护航组（与老板组同组时跳过）
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

        /// <summary>
        /// 把 hostileGroup 的全体存活成员加为 targetGroup 的敌人（组镜像敌意的单组传播，
        /// 统一传 Player 本体做键，与原生自然目击流程的 EnemyInfos 键一致）
        /// </summary>
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