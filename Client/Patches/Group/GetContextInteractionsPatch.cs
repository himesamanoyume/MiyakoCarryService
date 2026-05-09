
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Patches.BigSurvey;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Group
{
    /// <summary>
    /// 邀请至队伍界面对玩家添加右键自定义选项
    /// </summary>
    public sealed class GetContextInteractionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchmakerPlayerControllerClass), nameof(MatchmakerPlayerControllerClass.GetContextInteractions));

        private static Traverse _tarkovApplicationTraverse;
        public static bool IsMcsBotPlayerInventoryMode = false;
        public static string McsBotPlayerAid = "";

        [PatchPostfix]
        public static void Postfix(GroupPlayerDataClass player, ContextInteractionsClass __result)
        {
            if (IsMcsBotPlayerInventoryMode)
            {
                __result.method_2(
                    id: "BackMainChar",
                    key: "返回主角色",
                    callback: () => OnExitMcsBotPlayerInventoryMode(player.AccountId)
                );

                return;
            }

            __result.method_2(
                id: "OpenMcsBotPlayerInventoryMode",
                key: Locales.OPENMCSBOTPLAYERINVENTORY.McsLocalized(),
                callback: () => OnOpenMcsBotPlayerInventoryMode(player.AccountId)
            );
        }

        private static void OnExitMcsBotPlayerInventoryMode(string aid)
        {
            if (!McsRequestHandler.RemoveMcsBotPlayerAid(new() { Aid = aid }))
            {
                NotificationManagerClass.DisplayMessageNotification($"请选择当前护航库存模式的角色来返回主角色");
                return;
            }

            if (_tarkovApplicationTraverse == null)
            {
                TarkovApplication.Exist(out var tarkovApplication);
                _tarkovApplicationTraverse = Traverse.Create(tarkovApplication);
            }

            var mainMenuControllerClass = _tarkovApplicationTraverse.Field<MainMenuControllerClass>("mainMenuControllerClass").Value;

            McsBotPlayerAid = "";
            IsMcsBotPlayerInventoryMode = false;
            Singleton<PreloaderUI>.Instance.SetLoaderStatus(true);
            TasksExtensions.HandleExceptions(mainMenuControllerClass.method_21());
            MenuTaskBarAwakePatch.ShowMcsBotPlayerInventoryModeInfo(false);
        }

        private static void OnOpenMcsBotPlayerInventoryMode(string aid)
        {
            if (!McsRequestHandler.VerifyMcsBotPlayerAid(new() { Aid = aid }))
            {
                NotificationManagerClass.DisplayMessageNotification($"此玩家不是护航玩家，无法打开库存");
                return;
            }

            if (_tarkovApplicationTraverse == null)
            {
                TarkovApplication.Exist(out var tarkovApplication);
                _tarkovApplicationTraverse = Traverse.Create(tarkovApplication);
            }

            var mainMenuControllerClass = _tarkovApplicationTraverse.Field<MainMenuControllerClass>("mainMenuControllerClass").Value;

            McsBotPlayerAid = aid;
            IsMcsBotPlayerInventoryMode = true;
            Singleton<PreloaderUI>.Instance.SetLoaderStatus(true);
            TasksExtensions.HandleExceptions(mainMenuControllerClass.method_21());
            MenuTaskBarAwakePatch.ShowMcsBotPlayerInventoryModeInfo(true);
        }
    }

    public sealed class ContextInteractionsClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ContextInteractionsClass), nameof(ContextInteractionsClass.IsActive));

        [PatchPrefix]  
        public static bool Prefix(ref bool __result)  
        {  
            if (!GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode)  
            {  
                return true;
            }  
    
            __result = false;  
            return false;  
        }  
    }
}