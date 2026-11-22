using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Inventory
{
    /// <summary>
    /// 允许玩家对活着的玩家背包内的物品进行合并或拆分
    /// </summary>
    public sealed class ItemSubtract1Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(CG_SubtractFromDiscardLimits), nameof(CG_SubtractFromDiscardLimits.Method_Internal_Void_Item_0));

        [PatchPrefix]
        public static bool Prefix(CG_SubtractFromDiscardLimits __instance, Item itemToSubtract)
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
                var num2 = __instance.Method_Internal_Boolean_Item_byref_Int32_0(itemToSubtract, out var num3) ? num3 : itemToSubtract.StackObjectsCount;
                if (num2 == 0)
                {
                    return false;
                }
                __instance.__4__this.LogDiscardLimitsChange(itemToSubtract.Template, -num2);
                return false;
            }
            return true;
        }
    }

    public sealed class ItemSubtract2Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(CG_AddDiscardLimits), nameof(CG_AddDiscardLimits.Method_Internal_Void_Item_0));

        [PatchPrefix]
        public static bool Prefix(CG_AddDiscardLimits __instance, Item itemToAdd)
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

                foreach (var itemsCount in __instance.destroyedItems)
                {
                    if (itemsCount.Item == itemToAdd)
                    {
                        num = itemsCount.NumberToPreserve;
                        break;
                    }
                }
                if (num == 0)
                {
                    return false;
                }
                __instance.__4__this.LogDiscardLimitsChange(itemToAdd.Template, num);
                return false;
            }
            return true;
        }
    }
}
