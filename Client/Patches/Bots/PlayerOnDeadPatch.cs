
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 使服务端开启平衡限制时，清除护航的自带物品
    /// </summary>
    public sealed class PlayerOnDeadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.OnDead));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static void Prefix(Player __instance, EDamageType damageType)
        {
            if (!Tools.IsHost)
            {
                return;
            }

            if (!MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction)
            {
                return;
            }

            if (!McsMgr.IsMcsBotPlayer(__instance.ProfileId))
            {
                return;
            }

            HandleBalanceRestriction(__instance);
        }

        public static void HandleBalanceRestriction(Player player)
        {
            var slots = InventoryEquipment.AllSlotNames
                        .Where(slotName => slotName is not (EquipmentSlot.Backpack or EquipmentSlot.TacticalVest or EquipmentSlot.Pockets or EquipmentSlot.Dogtag))
                        .Select(player.AIData.BotOwner.Profile.Inventory.Equipment.GetSlot);

            foreach (var slot in slots)
            {
                if (slot == null || slot.ContainedItem == null)
                {
                    continue;
                }

                var itemData = slot.ContainedItem.GetData();
                if (itemData == null)
                {
                    continue;
                }

                if (itemData is not LootData lootData)
                {
                    continue;
                }

                if (lootData.VanishingCurse)
                {
                    slot.RemoveItemWithoutRestrictions();
                }
            }

            var allItems = player.AIData.BotOwner.Profile.Inventory.Equipment.GetAllItems();
            var allLootDatas = new List<LootData>();
            foreach (var item in allItems)
            {
                var itemData = item.GetData();
                if (itemData == null)
                {
                    continue;
                }

                if (itemData is not LootData lootData)
                {
                    continue;
                }

                if (lootData.VanishingCurse)
                {
                    allLootDatas.Add(lootData);
                }
            }

            foreach (var lootData in allLootDatas)
            {
                lootData.Item.McsRemoveItem();
            }
        }
    }
}