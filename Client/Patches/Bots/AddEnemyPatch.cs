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

        private static SquadMgr SquadMgr
        { 
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPrefix]
        public static bool Prefix(BotsGroup __instance, IPlayer person, EBotEnemyCause cause, ref bool __result)
        {
            if (person == null)
            {
                return true;
            }

            foreach (var botOwner in __instance.Members)
            {
                if (SquadMgr.IsMcsBotPlayer(botOwner.ProfileId))
                {
                    if (SquadMgr.IsMcsBossPlayer(person.ProfileId) || SquadMgr.IsMcsBotPlayer(person.ProfileId))
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
            if (person == null)
            {
                return;
            }

            string mcsBossPlayerId = null;
            if (SquadMgr.IsMcsBossPlayer(person.ProfileId))
            {
                mcsBossPlayerId = person.ProfileId;
            }
            else if (SquadMgr.IsMcsBotPlayer(person.ProfileId))
            {
                mcsBossPlayerId = SquadMgr.GetMcsBossPlayerByMcsBotPlayerId(person.ProfileId).GetPlayer.ProfileId;
            }

            if (mcsBossPlayerId == null)
            {
                return;
            }
            
            var allMcsMembers = SquadMgr.GetAllMcsSquadMembersByMcsBossId(mcsBossPlayerId);
    
            foreach (var member in allMcsMembers)
            {
                var botGroup = member.AIData?.BotOwner?.BotsGroup;
                if (botGroup == null || botGroup == __instance)
                {
                    continue;
                }

                foreach (var attacker in __instance.Members)
                {
                    botGroup.AddEnemy(attacker, EBotEnemyCause.addPlayerToBoss);
                }
            }
        }
    }
}