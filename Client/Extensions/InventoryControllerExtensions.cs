
using EFT.InventoryLogic;

namespace MiyakoCarryService.Client.Extensions
{
    internal static class InventoryControllerExtensions
    {
        extension(InventoryController inventoryController)
        {
            public void TakeLoot(Item item, bool isTargetItem)
            {
                if (item is Weapon)
                {
                    
                }
                else if (item is BackpackItemClass)
                {
                    
                }
                else if (item is VestItemClass && item.IsArmorMod())
                {
                    
                }
                else if (item is VestItemClass)
                {
                    
                }
                else if (item is HeadwearItemClass)
                {
                    
                }
                else if (item is ArmoredEquipmentItemClass)
                {
                    
                }
                else if (item is VisorsItemClass)
                {
                    
                }
                else if (item is ArmorPlateItemClass)
                {
                    
                }
                else if (item is OtherItemClass)
                {
                    
                }
            }

            public bool ShouldSwap(Item equippedItem, Item toSwapItem)
            {
                return true;
            }
        }
    }
}