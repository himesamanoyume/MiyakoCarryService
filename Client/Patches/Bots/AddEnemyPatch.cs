using System.Linq;
using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 避免护航Bot将护航老板当做敌人，同时让护航Bot立即将把护航老板或护航Bot视为敌人的敌人也视为敌人
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

            if (__instance.InitialBotType == WildSpawnType.shooterBTR && SquadMgr.IsMcsBotPlayer(person.ProfileId))
            {
                __result = false;
                return false;
            }

            var personRole = person.Profile?.Info?.Settings?.Role;
            if (personRole == null)
            {
                return true;
            }

            if (Classification.InitialBotEnemyCauses.Contains(cause) || Classification.FriendlyTypes.Contains(personRole.Value))
            {
                __result = false;
                return false;
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

            foreach (var botOwner in __instance.Members)
            {
                if (botOwner.Profile.Info.Settings.Role == WildSpawnType.shooterBTR)
                {
                    return;
                }

                if (SquadMgr.IsMcsBotPlayer(botOwner.ProfileId))
                {
                    if (SquadMgr.IsMcsBossPlayer(person.ProfileId) || SquadMgr.IsMcsBotPlayer(person.ProfileId))
                    {
                        return;
                    }
                }
            }

            if (SquadMgr.IsMcsBossPlayer(person.ProfileId))
            {
                var mcsMembers = SquadMgr.GetAllMcsSquadMembersByMcsBossId(person.ProfileId);
                var mcsBotsGroup = mcsMembers.FirstOrDefault().AIData.BotOwner.BotsGroup;
                if (mcsBotsGroup == null)
                {
                    return;
                }
                foreach (var botOwner in __instance.Members)
                {
                    mcsBotsGroup.AddEnemy(botOwner, EBotEnemyCause.addPlayerToBoss);
                }
            }
            else if (SquadMgr.IsMcsBotPlayer(person.ProfileId))
            {
                var mcsBotsGroup = person.AIData.BotOwner.BotsGroup;
                if (mcsBotsGroup == null)
                {
                    return;
                }
                foreach (var botOwner in __instance.Members)
                {
                    mcsBotsGroup.AddEnemy(botOwner, EBotEnemyCause.addPlayerToBoss);
                }
            }
        }
    }
}