using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 避免护航Bot将护航老板当做敌人，同时让护航Bot立即将把护航老板或护航Bot视为敌人的敌人也视为敌人，并且面对友好Bot类型时不主动开火
    /// </summary>
    internal sealed class AddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotsGroup), nameof(BotsGroup.AddEnemy));

        private static McsMgr McsMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<McsMgr>();
            }
        }

        [ThreadStatic]
        private static bool _isPropagating;

        [PatchPrefix]
        public static bool Prefix(BotsGroup __instance, IPlayer person, EBotEnemyCause cause, ref bool __result)
        {
            if (person == null)
            {
                return true;
            }

            if (__instance.DefWildSpawnType is WildSpawnType.shooterBTR or WildSpawnType.bossZryachiy or WildSpawnType.followerZryachiy)
            {
                if (McsMgr.IsMcsBotPlayer(person.ProfileId))
                {
                    __result = false;
                    return false;
                }
            }

            foreach (var botOwner in __instance.Members)
            {
                if (McsMgr.IsMcsBotPlayer(botOwner.ProfileId))
                {
                    if (person.Profile.Info.GroupId is "Mcs" or "Fika" || person.Profile.Info.Settings.Role is WildSpawnType.shooterBTR or WildSpawnType.bossZryachiy or WildSpawnType.followerZryachiy)
                    {
                        __result = false;
                        return false;
                    }

                    // if (!(person.Profile.Info.Settings.Role is WildSpawnType.assault or WildSpawnType.marksman or WildSpawnType.assaultGroup or WildSpawnType.pmcBEAR or WildSpawnType.pmcUSEC))
                    // {
                    //     MiyakoCarryServicePlugin.Logger.LogError($"护航 {botOwner.Profile.Info.Nickname} 正在尝试将 {person.Profile.Info.Settings.Role} 添加为敌人");
                    // }
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

            if (person.Profile.Info.GroupId is "Mcs" or "Fika")
            {
                return;
            }

            string mcsLeadPlayerId = null;
            if (McsMgr.IsMcsLeadPlayer(person.ProfileId))
            {
                mcsLeadPlayerId = person.ProfileId;
            }
            else if (McsMgr.IsMcsBotPlayer(person.ProfileId))
            {
                mcsLeadPlayerId = McsMgr.GetMcsLeadPlayerByMcsBotPlayerId(person.ProfileId).GetPlayer.ProfileId;
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

                    foreach (var attacker in __instance.Members)
                    {
                        // MiyakoCarryServicePlugin.Logger.LogError($"{attacker.Profile.Info.Nickname} 因老板被视为敌人即将调用AddEnemy");
                        botGroup.AddEnemy(attacker, EBotEnemyCause.byKill);
                    }
                }
            }
            finally
            {
                _isPropagating = false;
            }
        }
    }
}