using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.SAIN
{
    /// <summary>
    /// 避免SAIN中护航Bot将护航老板当做敌人
    /// </summary>
    internal sealed class IsPlayerFriendlyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => Type.GetType("SAIN.SAINComponent.Classes.EnemyClasses.EnemyListController, SAIN").GetMethod("IsPlayerFriendly", BindingFlags.Instance | BindingFlags.Public);

        private static SquadMgr SquadMgr
        { 
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPrefix]
        public static bool Prefix(IPlayer iPlayer, ref bool __result)
        {
            if (iPlayer == null)
            {
                return true;
            }

            if (SquadMgr.IsMcsLeadPlayer(iPlayer.ProfileId))
            {
                // MiyakoCarryServicePlugin.Logger.LogError("SAIN 认为McsLead 友好");
                __result = true;
                return false;
            }
            return true;
        }
    }

    internal sealed class TryAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => Type.GetType("SAIN.SAINComponent.Classes.EnemyClasses.EnemyListController, SAIN").GetMethod("tryAddEnemy", BindingFlags.Instance | BindingFlags.NonPublic);

        private static SquadMgr SquadMgr
        { 
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPrefix]
        public static bool Prefix(object __instance, IPlayer enemyPlayer, ref object __result)
        {
            if (enemyPlayer == null)
            {
                return true;
            }

            var botOwner = AccessTools.Property(Type.GetType("SAIN.SAINComponent.Classes.EnemyClasses.EnemyListController, SAIN"), "BotOwner")?.GetValue(__instance) as BotOwner;
            if (botOwner != null && SquadMgr.IsMcsBotPlayer(botOwner.ProfileId))
            {
                if (SquadMgr.IsMcsLeadPlayer(enemyPlayer.ProfileId))
                {
                    // MiyakoCarryServicePlugin.Logger.LogError("SAIN 没有添加老板为敌人");
                    __result = null;
                    return false;
                }
            }
            return true;
        }

        [PatchPostfix]
        public static void Postfix(object __instance, IPlayer enemyPlayer)
        {
            if (enemyPlayer == null)
            {
                return;
            }

            var botOwner = AccessTools.Property(Type.GetType("SAIN.SAINComponent.Classes.EnemyClasses.EnemyListController, SAIN"), "BotOwner")?.GetValue(__instance) as BotOwner;
            if (botOwner != null && SquadMgr.IsMcsBotPlayer(botOwner.ProfileId))
            {
                if (SquadMgr.IsMcsLeadPlayer(enemyPlayer.ProfileId))
                {
                    AccessTools.Method(Type.GetType("SAIN.SAINComponent.Classes.EnemyClasses.EnemyListController, SAIN"), "RemoveEnemy", [typeof(string)])?.Invoke(__instance, [enemyPlayer.ProfileId]);
                }
            }
        }
    }
}