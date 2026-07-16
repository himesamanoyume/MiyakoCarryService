using System.Reflection;
using Comfort.Common;
using EFT;
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
        protected override MethodBase GetTargetMethod() => AccessTools.PropertySetter(typeof(BindableStateClass<ActionsReturnClass>), nameof(BindableStateClass<ActionsReturnClass>.Value));

        [PatchPrefix]
        public static void Prefix(object __instance, ref ActionsReturnClass value)
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

            if (__instance is BindableStateClass<ActionsReturnClass> bindable)
            {
                if (ReferenceEquals(bindable.Gparam_0, value))
                {
                    bindable.Gparam_0 = null;
                }
            }
        }
    }
}