using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.SAIN
{
    /// <summary>
    /// 控制 CombatSoloLayer 是否应该激活
    /// </summary>
    public sealed class CombatSoloLayerIsActivePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(Type.GetType("SAIN.Layers.Combat.Solo.CombatSoloLayer, SAIN"), "IsActive");

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        private sealed class HysteresisState
        {
            public bool SainAllowed;
        }

        private static readonly ConditionalWeakTable<BotOwner, HysteresisState> _states = new();

        [PatchPrefix]
        public static bool Prefix(CustomLayer __instance, ref bool __result)
        {
            var botOwner = __instance.BotOwner;
            if (McsMgr.IsMcsBotPlayer(botOwner.ProfileId))
            {
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData == null)
                {
                    return true;
                }

                var state = _states.GetOrCreateValue(botOwner);
                if (mcsBotPlayerData.HasDecision(Decisions.ShouldUseStationaryWeapon))
                {
                    state.SainAllowed = false;
                    __result = false;
                    return false;
                }

                var goalEnemy = botOwner?.Memory?.GoalEnemy;
                if (goalEnemy == null)
                {
                    return true;
                }
                
                var mcsLeadPlayerPos = botOwner.GetMcsLeadPlayerPos(mcsBotPlayerData);
                var sqrDist = mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position);

                if (state.SainAllowed)
                {
                    if (sqrDist > SAINUtils.ExitSainSqr)
                    {
                        state.SainAllowed = false;
                    }
                }
                else
                {
                    if (sqrDist < SAINUtils.EnterSainSqr)
                    {
                        state.SainAllowed = true;
                    }
                }

                if (!state.SainAllowed)
                {
                    __result = false;
                    return false;
                }

                return true;
            }
            return true;
        }
    }
}
