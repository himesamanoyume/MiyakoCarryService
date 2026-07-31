using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 安装SAIN时，因为兼容性问题护航会极易丢失敌人信息，通过此Patch来阻止频繁丢失敌人目标信息
    /// </summary>
    public sealed class CalcGoalDropEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotCalcGoal), nameof(BotCalcGoal.method_0));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static void Prefix(BotCalcGoal __instance, ref bool withDropEnemy)
        {
            if (!withDropEnemy)
            {
                return;
            }

            if (__instance.BotOwner_0 == null || !McsMgr.IsMcsBotPlayer(__instance.BotOwner_0.ProfileId))
            {
                return;
            }

            var mcsBotPlayerData = __instance.BotOwner_0.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null && mcsBotPlayerData.HasDecision(Decisions.ShouldUseStationaryWeapon))
            {
                withDropEnemy = false;
            }
        }
    }
}