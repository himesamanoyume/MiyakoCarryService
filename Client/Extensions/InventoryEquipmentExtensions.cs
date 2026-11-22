
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
                return item != null && item.TryCast<Weapon>() != null;
            }

            public bool HasKnifeInSlot(EquipmentSlot slot)
            {
                if (inventoryEquipment == null)
                {
                    return false;
                }

                var item = inventoryEquipment.GetSlot(slot).ContainedItem;
                return item != null && item.TryCast<Knife>() != null;
            }
        }
    }
}