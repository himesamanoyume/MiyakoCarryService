using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>  
    /// 将交互选项列表的 Pivot 设为底部，使选项始终向上排布，防止遮挡 ItemPanel  
    /// </summary>  
    public sealed class ActionPanelAnchorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ActionPanel), nameof(ActionPanel.Start));

        [PatchPostfix]
        public static void Postfix(ActionPanel __instance)
        {
            __instance._interactionButtonsContainer.pivot = new(__instance._interactionButtonsContainer.pivot.x, 0f);
        }
    }
}