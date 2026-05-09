using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.Screens;
using HarmonyLib;
using MiyakoCarryService.Client.Patches.Group;
using SPT.Reflection.Patching;
using System.Reflection;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 根据是否处于护航库存模式决定是否使开始游戏不可用
    /// </summary>
    internal sealed class MenuScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MenuScreen), nameof(MenuScreen.method_6));

        private static Traverse _menuScreenTraverse = null;

        [PatchPostfix]
        public static void Postfix(MenuScreen __instance, EMatchingType matchingType)
        {
            if (GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode)
            {
                if (_menuScreenTraverse == null)
                {
                    _menuScreenTraverse = Traverse.Create(__instance);
                }

                var _playButton = _menuScreenTraverse.Field<DefaultUIButton>("_playButton").Value;
                _playButton.SetDisabledTooltip("正处于护航库存模式，无法进入战局", false);
                _playButton.Interactable = false;
                Singleton<PreloaderUI>.Instance.MenuTaskBar.SetCustomButtonsAvailability(new()
                {
                    {
                        EMenuType.Chat,
                        EStateSwitcher.Disabled
                    }
                });
            }
        }
    }
}
