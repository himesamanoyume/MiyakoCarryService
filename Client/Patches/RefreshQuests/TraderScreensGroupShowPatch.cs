
using System.Reflection;
using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.RefreshQuests
{
    internal sealed class TraderScreensGroupShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TraderScreensGroup), nameof(TraderScreensGroup.Show), [typeof(TraderScreensGroup.GClass3888)]);

        [PatchPrefix]
        public static void Prefix(TraderScreensGroup.GClass3888 controller)
        {
            MiyakoCarryServicePlugin.Logger.LogInfo("进行任务刷新");
            TarkovApplication.Exist(out var tarkovApplication);
            var tarkovApplicationTraverse = Traverse.Create(tarkovApplication);
            var mainMenuControllerClass = tarkovApplicationTraverse.Field<MainMenuControllerClass>("mainMenuControllerClass").Value;
            mainMenuControllerClass.LocalQuestControllerClass.QuestBookClass.InitRepeatableQuests(GameLoop.Instance.Session);
        }
    }
}