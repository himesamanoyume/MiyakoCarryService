
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace NotCheater.Client.Extensions;

internal static class LootDataExtensions
{
    extension(LootData lootData)
    {
        
        public void SetMoneyCurrency()
        {
            var templateId = lootData.Item.StringTemplateId;
            lootData.Offer = new TraderOffer("", lootData.Item.StackObjectsCount, lootData.Item.Height * lootData.Item.Width, templateId == CommonId.Euros ? ECurrencyType.EUR : templateId == CommonId.Dollars ? ECurrencyType.USD : templateId == CommonId.GPCoins ? ECurrencyType.GP : ECurrencyType.RUB);
        }

        // public bool IsLowPriceNotNoPriceNonContainerItem()
        // {
        //     lootData.IsLowPriceNotNoPriceNonContainerItem = !lootData.Item.IsContainer && IsLowPriceItem(lootData) && lootData.Offer.Price != 0;
        //     return lootData.IsLowPriceNotNoPriceNonContainerItem;
        // }

        public bool IsNoPriceNonContainerItem()
        {
            lootData.IsNoPriceNonContainerItem = !lootData.Item.IsContainer && lootData.Offer.Price == 0;
            return lootData.IsNoPriceNonContainerItem;
        }

        // public bool IsHighPriceItem()
        // {
        //     return lootData.Offer.Price >= NotCheaterPlugin.ItemPriceFilter.Value;
        // }

        public bool IsInSecureContainerItem()
        {
            var parentItem = lootData.Item.CurrentAddress?.Container?.ParentItem;
            return parentItem != null && ItemViewFactory.IsSecureContainer(parentItem);
        }

        // public bool CheckKeywordItem()
        // {
        //     var keywordArray = NotCheaterPlugin.KeywordItemText.Value;
        //     foreach (var keyword in keywordArray.Split(["||", ",", "，"], StringSplitOptions.RemoveEmptyEntries))
        //     {
        //         if (lootData.Item.ShortName.NotCheaterLocalized().Contains(keyword) || lootData.Item.Name.NotCheaterLocalized().Contains(keyword))
        //         {
        //             return true;
        //         }
        //     }
        //     return false;
        // }

        // public bool IsLowPriceItem()
        // {
        //     return lootData.Offer.CurrencyType == ECurrencyType.GP ? false : lootData.Offer.Price < NotCheaterPlugin.ItemPriceFilter.Value;
        // }

        // public bool CheckWishListItem(this ItemData itemData)
        // {
        //     var gameloop = GameLoop.Instance;
        //     if (gameloop.MyPlayer?.Profile?.WishlistManager == null)
        //     {
        //         return false;
        //     }

        //     return gameloop.MyPlayer.Profile.WishlistManager.IsInWishlist(itemData.Item.TemplateId, true, out EWishlistGroup eWishlistGroup);
        // }

        // public bool CheckQuestNeedItem(this ItemData itemData)
        // {
        //     var questDataMgr = GameLoop.Instance.GetMgr<QuestDataMgr>(EMgrType.QUEST);
        //     return questDataMgr.QuestNeedItemList.Contains(itemData.Item.StringTemplateId);
        // }

        public bool IsContainerItemAndNotItemInContainer()
        {
            lootData.IsContainerItemAndNotItemInContainer = lootData.Item.IsContainer && !lootData.IsItemInContainer;
            return lootData.IsContainerItemAndNotItemInContainer;
        }
    }
}