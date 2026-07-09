
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.RefreshQuests
{
    /// <summary>
    /// 用于在打开商人界面时刷新
    /// </summary>
    public sealed class TraderScreensGroupShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TraderScreensGroup), nameof(TraderScreensGroup.Show), [typeof(TraderScreensGroup.TraderScreenController)]);

        [PatchPostfix]
        public static void Postfix(TraderScreensGroup.TraderScreenController controller)
        {
            EventMgr.Notify(new UpdateProfileEvent());
            EventMgr.Notify(new UpdateDailyQuestsEvent());
        }
    }
}