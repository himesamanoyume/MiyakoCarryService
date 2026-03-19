
using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.RefreshQuests
{
    /// <summary>
    /// 用于阻止订阅特定事件以防止刷新每日任务的相关实例被释放
    /// </summary>
    internal sealed class InitRepeatableQuestsDisposePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(QuestBookClass), nameof(QuestBookClass.method_14));

        [PatchPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }
}