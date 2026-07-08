
using EFT.InventoryLogic;

namespace MiyakoCarryService.Client.Extensions
{
    public static class InventoryEquipmentExtensions
    {
        extension(InventoryEquipment inventoryEquipment)
        {
            public bool HasWeaponInSlot(EquipmentSlot slot)
            {
                if (inventoryEquipment == null)
                {
                    return false;
                }

                var item = inventoryEquipment.GetSlot(slot).ContainedItem;
                return item is Weapon;
            }

            public bool HasKnifeInSlot(EquipmentSlot slot)
            {
                if (inventoryEquipment == null)
                {
                    return false;
                }

                var item = inventoryEquipment.GetSlot(slot).ContainedItem;
                return item is Knife;
            }
        }
    }
}