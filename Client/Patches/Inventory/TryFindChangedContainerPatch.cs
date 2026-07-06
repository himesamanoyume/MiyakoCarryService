
using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;
using JetBrains.Annotations;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Inventory
{
    /// <summary>
    /// 让活着的人在被打开背包时，其装备可见
    /// </summary>
    public sealed class TryFindChangedContainerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SearchControllerClass), nameof(SearchControllerClass.TryFindChangedContainer));

        [PatchPostfix]
        public static void Postfix(ItemAddress address, [CanBeNull] out ContainerDataClass changedContainer, ref bool __result)
        {
            changedContainer = null;
            if (MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction)
            {
                return;
            }
            __result = false;
        }
    }
}