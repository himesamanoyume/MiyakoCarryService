using System;
using System.Reflection;
using DrakiaXYZ.BigBrain.Brains;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

public sealed class CombatSoloLayerIsActivePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(Type.GetType("SAIN.Layers.Combat.Solo.CombatSoloLayer, SAIN"), "IsActive");

    private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

    [PatchPrefix]
    public static bool Prefix(CustomLayer __instance, ref bool __result)
    {
        var botOwner = __instance.BotOwner;
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
            if (mcsBotPlayerData.HasDecision(Decisions.ShouldRegroup) || mcsBotPlayerData.HasDecision(Decisions.ShouldGoToPoint) || mcsBotPlayerData.HasDecision(Decisions.ShouldHoldPosition) || mcsBotPlayerData.HasDecision(Decisions.ShouldKeepFormation) || mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position) >= 35f * 35f)
            {
                __result = false;
                return false;
            }
            return true;
        }
        return true;

    }
}