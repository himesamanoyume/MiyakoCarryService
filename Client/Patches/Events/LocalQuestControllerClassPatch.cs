using System.Reflection;
using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 用于修复上交物品时任务条件计数器会被赋值过大导致的巨额退款问题
    /// </summary>
    public sealed class LocalQuestControllerClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(QuestControllerClientBackend.CG_HandoverItem), nameof(QuestControllerClientBackend.CG_HandoverItem.MoveNext));

        [PatchPostfix]
        public static void Postfix(QuestControllerClientBackend.CG_HandoverItem __instance)
        {
            var progressChecker = __instance.quest.ProgressCheckers[__instance.condition];
            if (progressChecker.Test())
            {
                var taskConditionCounter = __instance.QuestControllerClientBackend.Profile.GetTaskConditionCounter(__instance.quest, __instance.condition.id);
                taskConditionCounter.Value = (int)__instance.condition.value;
            }
        }
    }
}