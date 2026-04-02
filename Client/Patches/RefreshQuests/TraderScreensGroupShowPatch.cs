
using System;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.RefreshQuests
{
    /// <summary>
    /// 用于在打开任务界面时刷新
    /// </summary>
    public sealed class TraderScreensGroupShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TraderScreensGroup), nameof(TraderScreensGroup.Show), [typeof(TraderScreensGroup.GClass3888)]);

        private static Traverse _tarkovApplicationTraverse;

        [PatchPostfix]
        public static void Postfix(TraderScreensGroup.GClass3888 controller)
        {
            _ = UpdateDailyQuests();
        }

        private static async Task UpdateDailyQuests()
        {
            if (_tarkovApplicationTraverse == null)
            {
                TarkovApplication.Exist(out var tarkovApplication);
                _tarkovApplicationTraverse = Traverse.Create(tarkovApplication);
            }

            var mainMenuControllerClass = _tarkovApplicationTraverse.Field<MainMenuControllerClass>("mainMenuControllerClass").Value;
            var array = await GameLoop.Instance.Session.GetDailyQuests();
            if (!array.IsNullOrEmpty())
            {
                if (mainMenuControllerClass == null)
                {
                    _ = McsRequestHandler.SendLog("此为调试警报类型1，当你看到这条调试信息时，请到Discord频道 #发布 的子区中填写相应调查问卷，以帮助我修复Bug");
                    return;
                }

                if (mainMenuControllerClass.LocalQuestControllerClass == null)
                {
                    _ = McsRequestHandler.SendLog("此为调试警报类型2，当你看到这条调试信息时，请到Discord频道 #发布 的子区中填写相应调查问卷，以帮助我修复Bug");
                    return;
                }

                if (mainMenuControllerClass.LocalQuestControllerClass.QuestBookClass == null)
                {
                    _ = McsRequestHandler.SendLog("此为调试警报类型3，当你看到这条调试信息时，请到Discord频道 #发布 的子区中填写相应调查问卷，以帮助我修复Bug");
                    return;
                }
                mainMenuControllerClass.LocalQuestControllerClass.QuestBookClass.UpdateDailyQuests(array);
            }
        }
    }
}