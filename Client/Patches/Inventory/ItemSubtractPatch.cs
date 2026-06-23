using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Inventory
{
    /// <summary>
    /// 允许玩家对活着的玩家背包内的物品进行合并或拆分
    /// </summary>
    internal sealed class ItemSubtract1Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemSubtractClass1), nameof(ItemSubtractClass1.method_0));

        [PatchPrefix]
        public static bool Prefix(ItemSubtractClass1 __instance, Item itemToSubtract)
        {
            if (GameLoop.Instance.IsVaildGameWorld)
            {
                if (MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction)
                {
                    return true;
                }
                if (!itemToSubtract.LimitedDiscard)
                {
                    return false;
                }
                var num2 = __instance.method_1(itemToSubtract, out var num3) ? num3 : itemToSubtract.StackObjectsCount;
                if (num2 == 0)
                {
                    return false;
                }
                __instance.playerInventoryController_0.LogDiscardLimitsChange(itemToSubtract.Template, -num2);
                return false;
            }
            return true;
        }
    }

    internal sealed class ItemSubtract2Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemSubtractClass2), nameof(ItemSubtractClass2.method_0));

        [PatchPrefix]
        public static bool Prefix(ItemSubtractClass2 __instance, Item itemToAdd)
        {
            if (GameLoop.Instance.IsVaildGameWorld)
            {
                if (MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction)
                {
                    return true;
                }
                
                if (!itemToAdd.LimitedDiscard)
                {
                    return false;
                }
                var num = itemToAdd.StackObjectsCount;
                foreach (var destroyedItemsStruct in __instance.destroyedItems)
                {
                    destroyedItemsStruct.Deconstruct(out var item, out var num2, out var num3);
                    var item2 = item;
                    var num4 = num3;
                    if (item2 == itemToAdd)
                    {
                        num = num4;
                        break;
                    }
                }
                if (num == 0)
                {
                    return false;
                }
                __instance.playerInventoryController_0.LogDiscardLimitsChange(itemToAdd.Template, num);
                return false;
            }
            return true;
        }
    }
}
