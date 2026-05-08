
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Group
{
    /// <summary>
    /// 邀请至队伍界面对玩家添加右键按钮选项
    /// </summary>
    public sealed class GetContextInteractionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchmakerPlayerControllerClass), nameof(MatchmakerPlayerControllerClass.GetContextInteractions));

        [PatchPostfix]
        public static void Postfix(GroupPlayerDataClass player, ContextInteractionsClass __result)
        {
            __result.method_2(
                id: "OpenMcsBotPlayerInventory",
                key: Locales.OPENMCSBOTPLAYERINVENTORY.McsLocalized(),
                callback: () => OnOpenMcsBotPlayerInventory(player.AccountId)
            );
        }

        private static void OnOpenMcsBotPlayerInventory(string Aid)
        {
            NotificationManagerClass.DisplayMessageNotification($"正在尝试打开 {Aid} 的库存");
        }
    }
}