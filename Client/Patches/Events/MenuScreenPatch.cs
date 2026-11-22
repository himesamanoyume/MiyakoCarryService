
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Patches.Group;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;
using System.Reflection;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 缓存主菜单的“开始游戏”按钮实例
    /// </summary>
    public sealed class MenuScreenShowPatch : ModulePatch
    {
        private static DefaultUIButton _playButton;

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MenuScreen), nameof(MenuScreen.Show), [typeof(MenuScreen.MainMenuBaseScreenController)]);

        [PatchPostfix]
        public static void Postfix(MenuScreen __instance)
        {
            _playButton = __instance._playButton;
        }

        public static bool IsPlayButton(DefaultUIButton button) => button != null && button == _playButton;
    }

    /// <summary>
    /// 处于护航库存模式时，阻止任何“开始游戏”设为可交互的尝试
    /// </summary>
    public sealed class PlayButtonInteractableGuardPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(DefaultUIButton), "set_Interactable");

        [PatchPrefix]
        public static void Prefix(DefaultUIButton __instance, ref bool value)
        {
            if (!value || !GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode)
            {
                return;
            }

            if (MenuScreenShowPatch.IsPlayButton(__instance))
            {
                value = false;
                __instance.SetDisabledTooltip(Locales.CANNOTRAIDINMCSINVENTORYMODE.McsLocalized(), false);
            }
        }
    }
}