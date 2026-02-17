
using System.Reflection;
using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.RefreshQuests
{
    /// <summary>
    /// 用于在打开任务界面时刷新
    /// </summary>
    internal sealed class TraderScreensGroupShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TraderScreensGroup), nameof(TraderScreensGroup.Show), [typeof(TraderScreensGroup.GClass3888)]);

        private static MainMenuControllerClass mainMenuControllerClass;

        [PatchPrefix]
        public static void Prefix(TraderScreensGroup.GClass3888 controller)
        {
            if (mainMenuControllerClass == null)
            {
                TarkovApplication.Exist(out var tarkovApplication);
                var tarkovApplicationTraverse = Traverse.Create(tarkovApplication);
                mainMenuControllerClass = tarkovApplicationTraverse.Field<MainMenuControllerClass>("mainMenuControllerClass").Value;
            }
            _ = mainMenuControllerClass.LocalQuestControllerClass.QuestBookClass.Gclass4059_0.Run();
        }
    }
}