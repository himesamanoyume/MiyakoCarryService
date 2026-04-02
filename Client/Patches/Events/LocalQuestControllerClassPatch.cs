using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 用于修复上交物品时任务条件计数器会被赋值过大导致的巨额退款问题
    /// </summary>
    public sealed class LocalQuestControllerClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(LocalQuestControllerClass.Struct1295), nameof(LocalQuestControllerClass.Struct1295.MoveNext));

        [PatchPostfix]
        public static void Postfix(LocalQuestControllerClass.Struct1295 __instance)
        {
            var progressChecker = __instance.quest.ProgressCheckers[__instance.condition];
            if (progressChecker.Test())
            {
                var taskConditionCounter = __instance.LocalQuestControllerClass.Profile.GetTaskConditionCounter(__instance.quest, __instance.condition.id);
                taskConditionCounter.Value = (int)__instance.condition.value;
            }
        }
    }
}