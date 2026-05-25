

using System;
using System.Collections.Concurrent;
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
        public ConcurrentDictionary<BotOwner, bool> IsUsefulContainers = new();
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

        public bool IsUsefulContainer(BotOwner botOwner)
        {
            return IsUsefulContainers.TryGetValue(botOwner, out var isUsefulContainer) ? isUsefulContainer : false;
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
                if (botOwner != null)
                {
                    IsUsefulContainers.AddOrUpdate(botOwner, _botOwner => false, (_botOwner, oldIsUsefulContainer) =>
                    {
                        oldIsUsefulContainer = false;
                        return oldIsUsefulContainer;
                    });
                }
            }
            
            foreach (var kvp in IsUsefulContainers)
            {
                var isUseful = false;
                var botOwner = kvp.Key;
                var isAlive = botOwner.HealthController.IsAlive;

                if (!isAlive)
                {
                    continue;
                }

                var inventoryController = botOwner.GetPlayer.InventoryController;
                var currentSlot = LootData.ItemType == EItemType.Backpack ? inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack) : 
                    LootData.ItemType == EItemType.Rig ? inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest) : null;

                if (currentSlot == null)
                {
                    isUseful = true;
                }

                if (!isUseful)
                {
                    var currentContainer = currentSlot.ContainedItem;
                    if (currentContainer == null)
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

                IsUsefulContainers.AddOrUpdate(botOwner, _botOwner => isUseful, (_botOwner, oldIsUsefulContainer) =>
                {
                    oldIsUsefulContainer = isUseful;
                    return oldIsUsefulContainer;
                });
            }
        }
    }
}