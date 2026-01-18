

using EFT.InventoryLogic;
using MiyakoCarryService.Client.Utils;

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

        public override void UpdateAllLootInContainerInfo()
        {
            throw new System.NotImplementedException();
        }
    }
}