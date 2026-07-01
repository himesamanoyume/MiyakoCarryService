using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 让护航也能触发老板的TriggerEnter检测
    /// </summary>
    public sealed class TriggerWithIdEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TriggerWithId), nameof(TriggerWithId.TriggerEnter));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(TriggerWithId __instance, Player player)
        {
            var isMcsMemberPlayer = McsMgr.IsMcsMemberPlayer(player.ProfileId, out var mcsLeadPlayer);
            if (isMcsMemberPlayer && mcsLeadPlayer != null)
            {
                mcsLeadPlayer.AddTriggerZone(__instance);

                if (__instance is ExperienceTrigger experienceTrigger)
                {
                    var experienceTriggerTraverse = Traverse.Create(experienceTrigger);
                    mcsLeadPlayer.SpecialPlaceVisited(experienceTrigger.Id, experienceTriggerTraverse.Field<int>("_experience").Value);
                }
            }
        }
    }

    /// <summary>
    /// 让护航也能触发老板的TriggerExit检测
    /// </summary>
    public sealed class TriggerWithIdExitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TriggerWithId), nameof(TriggerWithId.TriggerExit));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(TriggerWithId __instance, Player player)
        {
            var isMcsMemberPlayer = McsMgr.IsMcsMemberPlayer(player.ProfileId, out var mcsLeadPlayer);
            if (isMcsMemberPlayer && mcsLeadPlayer != null)
            {
                mcsLeadPlayer.RemoveTriggerZone(__instance);
            }
        }
    }
}