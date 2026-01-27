

using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using NotCheater.Client.Extensions;

namespace MiyakoCarryService.Client.Datas
{
    internal sealed class LootData : ItemData
    {
        public TraderOffer Offer;
        public bool IsImportantContainer = false;
        public long TheMostExpensivePrice = 0;
        public bool IsItemInContainer = false;
        public bool IsWishListItem = false;
        public bool IsKeywordItem = false;
        public bool IsQuestNeedItem = false;
        public bool IsMoney = false;
        public bool IsLowPriceNotNoPriceNonContainerItem = false;
        public bool IsNoPriceNonContainerItem = false;
        public bool IsContainerItemAndNotItemInContainer = false;
        public bool IsLabyrinthSolvePuzzleItem = false;

        public LootData(Item item, TraderOffer offer) : base(item)
        {
            Offer = offer ?? new TraderOffer();
        }

        public void Reset()
        {
            CheckItemInteresting();
            ResetOffer();
        }

        public void CheckItemInteresting()
        {
            // IsWishListItem = this.CheckWishListItem();
            // IsQuestNeedItem = this.CheckQuestNeedItem();
            // IsKeywordItem = this.CheckKeywordItem();
            // IsMoney = Item.IsMoney();
        }

        public void ResetOffer()
        {
            // if (Item != null && Item.IsMoney())
            // {
            //     this.SetMoneyCurrency();
            // }
        }

        public override void UpdateAllLootInContainerInfo(McsBotPlayerConfig mcsBotPlayerConfig)
        {
            var theMostExpensivePrice = Offer.Price;
            IsImportantContainer = IsHighPriceItem;
            IsItemInContainer = false;
            ResetOffer();

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

                if (Tools.IsBlockItem((EBlockItemType)mcsBotPlayerConfig.BlockItemType, lootData.ItemType))
                {
                    continue;
                }

                if (lootData.IsInSecureContainerItem())
                {
                    continue;
                }

                TraderOffer offer;

                lootData.CheckItemInteresting();

                if (lootData.Item.IsMoney())
                {
                    SetMoneyCurrency();
                    offer = lootData.Offer;
                }
                else
                {
                    _gameloop.ItemBestPriceDict.TryGetValue(lootData.Item.StringTemplateId, out offer);
                }

                if (offer == null)
                {
                    offer = lootData.Item.ContainsBestPrice();
                }

                if (lootData.Offer.Price != offer.Price)
                {
                    lootData.Offer.Price = offer.Price;
                }

                if (lootData.Offer.Price > theMostExpensivePrice)
                {
                    theMostExpensivePrice = lootData.Offer.Price;
                }

                if (lootData.IsHighPriceItem)
                {
                    IsImportantContainer = true;
                    IsItemInContainer = true;
                }
            }

            TheMostExpensivePrice = theMostExpensivePrice;
        }

        public bool IsHighPriceItem => Offer.Price >= 100000f;

        public void SetMoneyCurrency()
        {
            var templateId = Item.StringTemplateId;
            var currency = templateId switch
            {
                CommonId.Euros => ECurrencyType.EUR,
                CommonId.Dollars => ECurrencyType.USD,
                CommonId.GPCoins => ECurrencyType.GP,
                _ => ECurrencyType.RUB
            };
            Offer = new TraderOffer(Item.StackObjectsCount, Item.Height * Item.Width, currency);
        }
    }
}