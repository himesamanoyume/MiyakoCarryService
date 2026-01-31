

using System.Collections.Generic;
using System.Linq;
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
    internal sealed class LootData : ItemData
    {
        public Dictionary<McsAIBossPlayer, LootProp> LootProps = new();
        public TraderOffer Offer;
        public bool IsItemInContainer = false;
        public bool IsMoney = false;
        public bool IsLabyrinthSolvePuzzleItem = false;
        public bool IsInSecureContainerItem = false;
        private Vector3 _lastPos = new();
        public bool IsNonNavigableItem
        {
            get
            {
                if (field)
                {
                    if (Transform.position == _lastPos)
                    {
                        field = true;
                        return field;
                    }
                    else
                    {
                        field = false;
                        return field;
                    }
                }
                field = false;
                return field;
            }
            set
            {
                field = value;
                if (field)
                {
                    _lastPos = Transform.position;
                }
            }
        }

        public LootData(Item item, TraderOffer offer) : base(item)
        {
            Offer = offer ?? new TraderOffer();
        }

        public void Refresh(McsAIBossPlayer mcsAIBossPlayer)
        {
            ResetOffer();
            CheckItemInteresting(mcsAIBossPlayer);
            CheckSecureContainerItem();
        }

        public void CheckItemInteresting(McsAIBossPlayer mcsAIBossPlayer)
        {
            if (!LootProps.TryGetValue(mcsAIBossPlayer, out var lootProp))
            {
                lootProp = new LootProp(Item, Offer, mcsAIBossPlayer);
                LootProps[mcsAIBossPlayer] = lootProp;
            }
            lootProp.CheckWishListItem();
            lootProp.CheckHighPriceItem();
            lootProp.CheckBlockItem(ItemType);
            // IsQuestNeedItem = this.CheckQuestNeedItem();
            // IsKeywordItem = this.CheckKeywordItem();
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
            }
            IsInSecureContainerItem = false;
        }

        public override void RefreshInteresting(McsAIBossPlayer mcsAIBossPlayer)
        {
            IsItemInContainer = false;
            Refresh(mcsAIBossPlayer);

            if (ItemsInContainer == null)
            {
                ItemsInContainer = Item.GetAllDatas().ToList();
            }

            foreach (var itemData in ItemsInContainer)
            {
                if (itemData == null)
                {
                    continue;
                }

                if (itemData.Item.Id == Item.Id)
                {
                    continue;
                }

                if (this == itemData)
                {
                    continue;
                }

                if (itemData is not LootData lootData)
                {
                    continue;
                }

                lootData.Refresh(mcsAIBossPlayer);
                IsItemInContainer = true;
            }
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

        protected override Transform GetTransfrom()
        {
            try
            {
                if (Item == null || Item.Owner == null)
                {
                    return null;
                }

                var gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld == null)
                {
                    return null;
                }

                if (!gameWorld.ItemOwners.TryGetValue(Item.Owner, out var itemTransform))
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
    }
}