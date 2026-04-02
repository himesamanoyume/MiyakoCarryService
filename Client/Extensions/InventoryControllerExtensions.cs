
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Client.Extensions
{
    public static class InventoryControllerExtensions
    {
        extension(InventoryController inventoryController)
        {
            public void TakeLoot(McsBotPlayerConfig mcsBotPlayerConfig, Item item, bool isTargetItem)
            {
                if (item is Weapon)
                {
                    
                }
                else if (item is BackpackItemClass)
                {
                    
                }
                else if (item is VestItemClass)
                {
                    if (item.IsArmorMod())
                    {
                        
                    }
                    else
                    {
                        
                    }
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

            public bool ShouldSwap(McsBotPlayerConfig mcsBotPlayerConfig, Item equippedItem, Item toSwapItem)
            {
                return true;
            }

            public void ThrowAndEquip()
            {
                
            }
        }
    }
}