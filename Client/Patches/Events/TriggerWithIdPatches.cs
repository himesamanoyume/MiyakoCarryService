using System.Reflection;
using Comfort.Common;
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

        private static CommandMgr CommandMgr => MgrAccessor.Get<CommandMgr>();

        [PatchPostfix]
        public static void Postfix(TriggerWithId __instance, Player player)
        {
            var isMcsMemberPlayer = CommandMgr.IsMcsMemberPlayer(player.ProfileId);
            if (isMcsMemberPlayer)
            {
                var mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                mcsLeadPlayer.AddTriggerZone(__instance);

                if (__instance is ExperienceTrigger experienceTrigger)
                {
                    var experienceTriggerTraverse = Traverse.Create(experienceTrigger);
                    mcsLeadPlayer.SpecialPlaceVisited(experienceTrigger.Id, experienceTriggerTraverse.Field<int>("_experience").Value);
                }
                else if (__instance is PlaceItemTrigger placeItemTrigger)
                {
                    mcsLeadPlayer.OnPlaceItemTriggerChanged(placeItemTrigger);
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

        private static CommandMgr CommandMgr => MgrAccessor.Get<CommandMgr>();

        [PatchPostfix]
        public static void Postfix(TriggerWithId __instance, Player player)
        {
            var isMcsMemberPlayer = CommandMgr.IsMcsMemberPlayer(player.ProfileId);
            if (isMcsMemberPlayer)
            {
                var mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                mcsLeadPlayer.RemoveTriggerZone(__instance);

                if (__instance is PlaceItemTrigger)
                {
                    mcsLeadPlayer.OnPlaceItemTriggerChanged(null);
                }
            }
        }
    }
}