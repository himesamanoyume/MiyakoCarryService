using System.Reflection;
using EFT.Quests;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 用于修复上交物品时任务条件计数器会被赋值过大导致的巨额退款问题
    /// </summary>
    public sealed class LocalQuestControllerClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(QuestControllerClientBackend), nameof(QuestControllerClientBackend.HandoverItem));

        [PatchPostfix]
        public static void Postfix(Quest quest, ConditionItem condition)
        {
            var progressChecker = quest.ProgressCheckers[condition];
            if (progressChecker.Test())
            {
                var taskConditionCounter = GameLoop.Instance.Session.Profile.GetTaskConditionCounter(quest, condition.id);
                taskConditionCounter.Value = (int)condition.value;
            }
        }
    }
}