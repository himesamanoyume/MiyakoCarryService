

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        public List<Player> McsSquadMembers
        {
            get
            {
                return field ??= McsMgr.GetAllMcsSquadMembersByMcsLeadId(McsAILeadPlayer.McsLeadPlayer.ProfileId).ToList();
            }
        }
        private readonly ConcurrentDictionary<BotOwner, bool> _isShouldTakeContainers = new();
        private readonly ConcurrentDictionary<BotOwner, bool> _isShouldSwapContainers = new();
        private readonly ConcurrentDictionary<BotOwner, bool> _isShouldEquipContainers = new();
        private readonly ConcurrentDictionary<BotOwner, ENestType> _isShouldNestContainers = new();
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
            IsBlockItem = Tools.IsBlockItem((EBlockItemType)McsAILeadPlayer.McsBotPlayerConfig.BlockItemType, LootData);
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

        public ENestType IsShouldNestContainer(BotOwner botOwner)
        {
            return _isShouldNestContainers.TryGetValue(botOwner, out var nestType) ? nestType : ENestType.None;
        }

        public void UpdateContainerProp(ConcurrentDictionary<BotOwner, bool> kvp, BotOwner botOwner, bool value)
        {
            kvp.AddOrUpdate(botOwner, _botOwner => false, (_botOwner, oldValue) =>
            {
                oldValue = value;
                return oldValue;
            });
        }

        public void UpdateContainerProp(ConcurrentDictionary<BotOwner, ENestType> kvp, BotOwner botOwner, ENestType value)
        {
            kvp.AddOrUpdate(botOwner, _botOwner => ENestType.None, (_botOwner, oldValue) =>
            {
                oldValue = value;
                return oldValue;
            });
        }

        public void CheckUsefulContainer()
        {
            foreach (var mcsBotPlayer in McsSquadMembers)
            {
                var botOwner = mcsBotPlayer.BotOwner;
                if (botOwner == null)
                {
                    Reset(botOwner);
                    continue;
                }

                if (!botOwner.HealthController.IsAlive)
                {
                    Reset(botOwner);
                    continue;
                }

                var inventoryController = mcsBotPlayer.GetPlayer.InventoryController;
                var currentSlot = LootData.ItemType == EItemType.Backpack ? inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack) : 
                    LootData.ItemType == EItemType.Equipment ? inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest) : null;

                if (currentSlot == null)
                {
                    Reset(botOwner);
                    continue;
                }

                var currentContainer = currentSlot.ContainedItem;
                if (currentContainer == null)
                {
                    UpdateContainerProp(_isShouldEquipContainers, botOwner, true);
                    UpdateContainerProp(_isShouldNestContainers, botOwner, ENestType.None);
                    UpdateContainerProp(_isShouldSwapContainers, botOwner, false);
                    UpdateContainerProp(_isShouldTakeContainers, botOwner, false);
                    continue;
                }

                var itemData = currentContainer.GetData();
                if (itemData == null || itemData is not LootData currentLootData)
                {
                    Reset(botOwner);
                    continue;
                }

                var shouldNestIn = currentLootData.MaxSingleGridCount >= LootData.ItemGridCount;
                var shouldNestOut = LootData.MaxSingleGridCount >= currentLootData.ItemGridCount;

                if (shouldNestOut || shouldNestIn)
                {
                    UpdateContainerProp(_isShouldEquipContainers, botOwner, false);
                    if (shouldNestOut)
                    {
                        UpdateContainerProp(_isShouldNestContainers, botOwner, ENestType.Out);
                    }
                    else
                    {
                        UpdateContainerProp(_isShouldNestContainers, botOwner, ENestType.In);
                    }
                    UpdateContainerProp(_isShouldSwapContainers, botOwner, false);
                    UpdateContainerProp(_isShouldTakeContainers, botOwner, false);
                    continue;
                }

                if (LootData.ContainerGridCount > currentLootData.ContainerGridCount)
                {
                    UpdateContainerProp(_isShouldEquipContainers, botOwner, false);
                    UpdateContainerProp(_isShouldNestContainers, botOwner, ENestType.None);
                    UpdateContainerProp(_isShouldSwapContainers, botOwner, true);
                    UpdateContainerProp(_isShouldTakeContainers, botOwner, false);
                    continue;
                }

                if (LootData.IsContainerWithAdditionalGrid)
                {
                    UpdateContainerProp(_isShouldEquipContainers, botOwner, false);
                    UpdateContainerProp(_isShouldNestContainers, botOwner, ENestType.None);
                    UpdateContainerProp(_isShouldSwapContainers, botOwner, false);
                    UpdateContainerProp(_isShouldTakeContainers, botOwner, true);
                    continue;
                }

                Reset(botOwner);
            }
        }

        private void Reset(BotOwner botOwner)
        {
            UpdateContainerProp(_isShouldEquipContainers, botOwner, false);
            UpdateContainerProp(_isShouldNestContainers, botOwner, ENestType.None);
            UpdateContainerProp(_isShouldSwapContainers, botOwner, false);
            UpdateContainerProp(_isShouldTakeContainers, botOwner, false);
        }
    }
}