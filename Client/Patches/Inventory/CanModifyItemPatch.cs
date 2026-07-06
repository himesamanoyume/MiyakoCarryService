using SPT.Reflection.Patching;
using System.Reflection;
using EFT.InventoryLogic;
using EFT;
using Diz.LanguageExtensions;
using System.Collections.Generic;
using HarmonyLib;

namespace MiyakoCarryService.Client.Patches.Inventory
{
    /// <summary>
    /// 使打开护航背包指令打开护航背包时，允许移动护航背包里的物品
    /// </summary>
    public sealed class CanModifyItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.CanModifyItem));

        [PatchPrefix]
        public static bool Prefix(Item item, ItemAddress from, TraderControllerClass controller, ref Error error, ref bool __result)
        {
            if (!GameLoop.Instance.IsVaildGameWorld)
            {
                return true;
            }

            if (MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction)
            {
                return true;
            }

            var combinedList = new List<Item>
            {
                item
            };

            foreach (var parent in item.GetAllParentItems(true))
            {
                combinedList.Add(parent);
            }

            foreach (var _item in combinedList)
            {
                if (_item.PinLockState == EItemPinLockState.Locked)
                {
                    error = new ItemLockedClass(item);
                    __result = false;
                    return false;
                }
            }

            if (from.GetOwner() != controller && from.IsSpecialSlotAddress())
            {
                error = new CannotMoveItemDuringRaidClass(item, from.Container.ID);
                __result = false;
                return false;
            }
            
            var observerItemState = controller.SearchController.GetObserverItemState(item, from);
            error = observerItemState == EObserverItemState.Known ? null : new UnknownItemManipulationClass(item, observerItemState);
            __result = true;
            return false;
        }
    }
}
