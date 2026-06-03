using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.SAIN
{
    /// <summary>
    /// 当敌人距离较远时，阻止护航使用SAIN的层级，防止护航远离
    /// </summary>
    public class BotDecisionManagerGetDecisionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(Type.GetType("SAIN.SAINComponent.Classes.Decision.BotDecisionManager, SAIN"), "getDecision");

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        private static Type _combatDecisionType
        {
            get
            {
                return field ??= Type.GetType("SAIN.ECombatDecision, SAIN");
            }
        }
        private static Type _squadDecisionType
        {
            get
            {
                return field ??= Type.GetType("SAIN.ESquadDecision, SAIN");
            }
        }
        private static Type _selfActionType
        {
            get
            {
                return field ??= Type.GetType("SAIN.ESelfActionType, SAIN");
            }
        }
        private static Type _enemyType
        {
            get
            {
                return field ??= Type.GetType("SAIN.SAINComponent.Classes.EnemyClasses.Enemy, SAIN");
            }
        }
        private static object _combatDecisionValue
        {
            get
            {
                return field ??= Enum.Parse(_combatDecisionType, "None");
            }
        }
        private static object _squadDecisionValue
        {
            get
            {
                return field ??= Enum.Parse(_squadDecisionType, "None");
            }
        }
        private static object _selfActionTypeValue
        {
            get
            {
                return field ??= Enum.Parse(_selfActionType, "None");
            }
        }

        [PatchPrefix]
        public static bool Prefix(object __instance)
        {
            var botDecisionManagerTraverse = Traverse.Create(__instance);
            var botOwner = botDecisionManagerTraverse.Property("BotOwner").GetValue<BotOwner>();
            if (McsMgr.IsMcsBotPlayer(botOwner.ProfileId))
            {
                var goalEnemy = botOwner?.Memory?.GoalEnemy;
                if (goalEnemy == null)
                {
                    return true;
                }

                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData == null)
                {
                    return true;
                }

                var mcsLeadPlayerPos = botOwner.GetMcsLeadPlayerPos(mcsBotPlayerData);
                if (mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position) >= 35f * 35f)
                {
                    botDecisionManagerTraverse.Method("SetDecisions", [_combatDecisionType, _squadDecisionType, _selfActionType, _enemyType]).GetValue([_combatDecisionValue, _squadDecisionValue, _selfActionTypeValue, null]);
                    return false;
                }
                return true;
            }
            return true;
        }
    }
}