

using System;
using System.Collections.Concurrent;
using EFT;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Datas
{
    public class LootProp
    {
        public LootData LootData;
        public McsAILeadPlayer McsAILeadPlayer;
        public TraderOffer Offer;
        public bool IsHighPriceItem = false;
        public bool IsKeywordItem = false;
        public bool IsBlockItem = false;
        private readonly ConcurrentDictionary<BotOwner, bool> _isShouldTakeContainers = new();
        private readonly ConcurrentDictionary<BotOwner, bool> _isShouldSwapContainers = new();
        private readonly ConcurrentDictionary<BotOwner, bool> _isShouldEquipContainers = new();
        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        public LootProp(LootData lootData, TraderOffer offer, McsAILeadPlayer mcsAILeadPlayer)
        {
            LootData = lootData;
            Offer = offer;
            McsAILeadPlayer = mcsAILeadPlayer;
        }

        public void CheckHighPriceItem()
        {
            IsHighPriceItem = Offer.Price >= McsAILeadPlayer.McsBotPlayerConfig.PriceThreshold;
        }

        public void CheckKeywordItem()
        {
            var keywordArray = McsAILeadPlayer.McsBotPlayerConfig.KeywordItemText;
            foreach (var keyword in keywordArray.Split(["||", ",", "，"], StringSplitOptions.RemoveEmptyEntries))
            {
                if (LootData.Item.ShortName.McsLocalized().Contains(keyword) || LootData.Item.Name.McsLocalized().Contains(keyword))
                {
                    IsKeywordItem = true;
                    return;
                }
            }
            IsKeywordItem = false;
        }

        public void CheckBlockItem()
        {
            IsBlockItem = Tools.IsBlockItem((EBlockItemType)McsAILeadPlayer.McsBotPlayerConfig.BlockItemType, LootData.ItemType);
        }

        public bool IsShouldTakeContainer(BotOwner botOwner)
        {
            return _isShouldTakeContainers.TryGetValue(botOwner, out var isUsefulContainer) ? isUsefulContainer : false;
        }

        public bool IsShouldSwapContainer(BotOwner botOwner)
        {
            return _isShouldSwapContainers.TryGetValue(botOwner, out var isShouldSwapContainer) ? isShouldSwapContainer : false;
        }

        public bool IsShouldEquipContainer(BotOwner botOwner)
        {
            return _isShouldEquipContainers.TryGetValue(botOwner, out var isShouldEquipContainer) ? isShouldEquipContainer : false;
        }

        public void UpdateContainerProp(ConcurrentDictionary<BotOwner, bool> kvp, BotOwner botOwner, bool value)
        {
            kvp.AddOrUpdate(botOwner, _botOwner => false, (_botOwner, oldValue) =>
            {
                oldValue = value;
                return oldValue;
            });
        }

        public void CheckUsefulContainer()
        {
            var mcsBotPlayerBotOwners = McsMgr.GetAllMcsSquadMembersByMcsLeadId(McsAILeadPlayer.McsLeadPlayer.ProfileId);
            if (mcsBotPlayerBotOwners == null)
            {
                return;
            }

            foreach (var botOwner in mcsBotPlayerBotOwners)
            {
                if (botOwner == null)
                {
                    continue;
                }

                UpdateContainerProp(_isShouldTakeContainers, botOwner, false);
                UpdateContainerProp(_isShouldSwapContainers, botOwner, false);
                UpdateContainerProp(_isShouldEquipContainers, botOwner, false);

                if (!botOwner.HealthController.IsAlive)
                {
                    continue;
                }

                var inventoryController = botOwner.GetPlayer.InventoryController;
                var currentSlot = LootData.ItemType == EItemType.Backpack ? inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack) : 
                    LootData.ItemType == EItemType.Rig ? inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest) : null;

                if (currentSlot == null)
                {
                    continue;
                }

                var currentContainer = currentSlot.ContainedItem;
                if (currentContainer == null)
                {
                    UpdateContainerProp(_isShouldEquipContainers, botOwner, true);
                    continue;
                }

                var itemData = currentContainer.GetData();
                if (itemData == null || itemData is not LootData currentLootData)
                {
                    continue;
                }

                if (LootData.ItemGridCount > currentLootData.ItemGridCount)
                {
                    UpdateContainerProp(_isShouldSwapContainers, botOwner, true);
                    continue;
                }

                if (LootData.IsContainerWithAdditionalGrid)
                {
                    UpdateContainerProp(_isShouldTakeContainers, botOwner, true);
                }
            }
        }
    }
}