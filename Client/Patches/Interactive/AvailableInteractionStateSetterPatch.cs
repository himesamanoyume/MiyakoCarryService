using System.Reflection;
using Comfort.Common;
using Diz.Binding;
using EFT;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Interactive
{
    /// <summary>  
    /// 优化指令菜单，使其只有在主动关闭指令菜单时才会关闭
    /// </summary>  
    public sealed class AvailableInteractionStateSetterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.PropertySetter(typeof(BindableState<AvailableInteractionState>), nameof(BindableState<AvailableInteractionState>.Value));

        [PatchPrefix]
        public static void Prefix(object __instance, ref AvailableInteractionState value)
        {
            if (!CommandUtils.IsCommandMenuOpen)
            {
                return;
            }

            if (!Singleton<GameWorld>.Instantiated || Singleton<GameWorld>.Instance?.MainPlayer == null)
            {
                return;
            }

            var owner = CommandUtils.GamePlayerOwner;

            if (owner == null || !ReferenceEquals(__instance, owner.AvailableInteractionState))
            {
                return;
            }

            var isOurMenu = ReferenceEquals(value, CommandUtils.CurrentMenu);

            if (!isOurMenu)
            {
                CommandUtils.MergeIncomingIntoCommandMenu(value);
                value = CommandUtils.CurrentMenu;
            }

            if (__instance is BindableState<AvailableInteractionState> bindable)
            {
                if (ReferenceEquals(bindable.gparam_0, value))
                {
                    bindable.gparam_0 = null;
                }
            }
        }
    }
}