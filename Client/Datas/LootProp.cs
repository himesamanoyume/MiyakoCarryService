

using EFT.InventoryLogic;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Datas
{
    internal class LootProp
    {
        public Item Item;
        public McsAILeadPlayer McsAILeadPlayer;
        public TraderOffer Offer;
        public bool IsHighPriceItem = false;
        public bool IsWishListItem = false;
        public bool IsQuestNeedItem = false;
        public bool IsBlockItem = false;
        

        public LootProp(Item item, TraderOffer offer, McsAILeadPlayer mcsAILeadPlayer)
        {
            Item = item;
            Offer = offer;
            McsAILeadPlayer = mcsAILeadPlayer;
        }

        public void CheckHighPriceItem()
        {
            IsHighPriceItem = Offer.Price >= McsAILeadPlayer.McsBotPlayerConfig.PriceThreshold;
        }

        public void CheckWishListItem()
        {
            var wishlistManager = McsAILeadPlayer.Player().Profile.WishlistManager;
            if (wishlistManager != null)
            {
                IsWishListItem = wishlistManager.IsInWishlist(Item.TemplateId, true, out _);
            }
            IsWishListItem = false;
        }

        public void CheckBlockItem(EItemType itemType)
        {
            IsBlockItem = Tools.IsBlockItem((EBlockItemType)McsAILeadPlayer.McsBotPlayerConfig.BlockItemType, itemType);
        }

        // public bool CheckQuestNeedItem()
        // {

        // }
    }
}