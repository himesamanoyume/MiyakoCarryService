

using System;
using System.Collections.Concurrent;
using System.Linq;
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
        public ConcurrentDictionary<McsBotPlayerData, bool> IsUsefulContainers;

        public LootProp(LootData lootData, TraderOffer offer, McsAILeadPlayer mcsAILeadPlayer)
        {
            LootData = lootData;
            Offer = offer;
            McsAILeadPlayer = mcsAILeadPlayer;
            var mcsMgr = MgrAccessor.Get<McsMgr>();
            var mcsBotPlayerBotOwners = mcsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(McsAILeadPlayer.McsLeadPlayer.ProfileId);
            foreach (var botOwner in mcsBotPlayerBotOwners)
            {
                IsUsefulContainers.TryAdd(botOwner.GetMcsBotPlayerData(), false);
            }
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

        public bool IsUsefulContainer(McsBotPlayerData mcsBotPlayerData)
        {
            return IsUsefulContainers.TryGetValue(mcsBotPlayerData, out var isUsefulContainer) ? isUsefulContainer : false;
        }

        public void CheckUsefulContainer()
        {
            foreach (var kvp in IsUsefulContainers)
            {
                var isUseful = false;
                var mcsBotPlayerData = kvp.Key;
                var isAlive = mcsBotPlayerData.BotOwner.HealthController.IsAlive;

                if (!isUseful && isAlive)
                {
                    var inventoryController = mcsBotPlayerData.Player.InventoryController;
                    var currentSlot = LootData.ItemType == EItemType.Backpack ? inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack) : 
                        LootData.ItemType == EItemType.Rig ? inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest) : null;

                    if (currentSlot == null)
                    {
                        isUseful = true;
                    }

                    var currentContainer = currentSlot.ContainedItem;
                    if (!isUseful && currentContainer == null)
                    {
                        isUseful = true;
                    }

                    if (!isUseful && currentContainer is SearchableItemItemClass searchableItemItemClass)
                    {
                        var currentContainerGrid = searchableItemItemClass.Grids.FirstOrDefault(); 
                        var currentContainerGridCOunt = currentContainerGrid.GridWidth * currentContainerGrid.GridHeight;
                        if (LootData.ContainerGridCount > currentContainerGridCOunt)
                        {
                            isUseful = true;
                        }
                    }
                }

                IsUsefulContainers.AddOrUpdate(mcsBotPlayerData, _mcsBotPlayerData => isUseful, (_mcsBotPlayerData, oldIsUsefulContainer) =>
                {
                    oldIsUsefulContainer = isUseful;
                    return oldIsUsefulContainer;
                });
            }
        }
    }
}