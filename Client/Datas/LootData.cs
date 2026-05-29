

using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public sealed class LootData : ItemData
    {
        public Dictionary<McsAILeadPlayer, LootProp> LootProps = new();
        public TraderOffer Offer;
        public bool IsItemInContainer = false;
        public bool IsMoney = false;
        public bool IsInSecureContainerItem = false;
        public int ItemGridCount => Item.Width * Item.Height;
        public int ContainerGridCount = 0;
        public int MaxSingleGridCount = 0;
        public bool IsContainerWithAdditionalGrid => ContainerGridCount > ItemGridCount;
        public bool IsEquipableContainer => ItemType == EItemType.Backpack || (ItemType == EItemType.Equipment && Item is not HeadphonesItemClass);

        public LootData(Item item, TraderOffer offer) : base(item)
        {
            Offer = offer ?? new TraderOffer();
            if (Item.IsContainer && Item is SearchableItemItemClass containerItem)
            {
                foreach (var stashGridClass in containerItem.Grids)
                {
                    var singleGridCount = stashGridClass.GridWidth * stashGridClass.GridHeight;
                    ContainerGridCount += singleGridCount;
                    if (singleGridCount > MaxSingleGridCount)
                    {
                        MaxSingleGridCount = singleGridCount;
                    }
                }
            }
        }

        public void Refresh(McsAILeadPlayer mcsAILeadPlayer)
        {
            ResetOffer();
            CheckSecureContainerItem();
            CheckItemInteresting(mcsAILeadPlayer);
        }

        public void CheckItemInteresting(McsAILeadPlayer mcsAILeadPlayer)
        {
            if (!LootProps.TryGetValue(mcsAILeadPlayer, out var lootProp))
            {
                lootProp = new LootProp(this, Offer, mcsAILeadPlayer);
                LootProps[mcsAILeadPlayer] = lootProp;
            }
            lootProp.CheckKeywordItem();
            lootProp.CheckHighPriceItem();
            lootProp.CheckBlockItem();
            if (IsEquipableContainer)
            {
                lootProp.CheckUsefulContainer();
            }
        }

        public void ResetOffer()
        {
            IsMoney = Item.IsMoney();
            if (IsMoney)
            {
                SetMoneyCurrency();
            }
        }

        public void CheckSecureContainerItem()
        {
            var parentItem = Item.CurrentAddress?.Container?.ParentItem;
            if (parentItem != null && ItemViewFactory.IsSecureContainer(parentItem))
            {
                IsInSecureContainerItem = true;
                return;
            }
            IsInSecureContainerItem = false;
        }

        public override void RefreshInteresting(McsAILeadPlayer mcsAILeadPlayer, bool unlock)
        {
            Refresh(mcsAILeadPlayer);
            base.RefreshInteresting(mcsAILeadPlayer, unlock);
        }

        public void SetMoneyCurrency()
        {
            var currency = Item.StringTemplateId switch
            {
                CommonId.Euros => ECurrencyType.EUR,
                CommonId.Dollars => ECurrencyType.USD,
                CommonId.GPCoins => ECurrencyType.GP,
                _ => ECurrencyType.RUB
            };
            Offer.Price = Item.StackObjectsCount;
            Offer.CurrencyType = currency;
        }

        protected override Transform GetRootTransfrom()
        {
            try
            {
                if (Item == null)
                {
                    return null;
                }

                var rootItem = Item.GetRootItem();
                if (rootItem == null || rootItem.Owner == null)
                {
                    return null;
                }

                var gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld == null)
                {
                    return null;
                }

                if (!gameWorld.ItemOwners.TryGetValue(rootItem.Owner, out var itemTransform))
                {
                    return null;
                }

                var transform = itemTransform.Transform;
                if (transform == null)
                {
                    return null;
                }

                return transform;
            }
            catch
            {
                return null;
            }
        }

        public bool IsLocked()
        {
            return _lootDataMgr.IsLockedLootingTarget(this) && _lootDataMgr.IsLockedLootingTargetRootTransform(RootTransform);
        }
    }
}