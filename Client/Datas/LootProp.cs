

using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Datas
{
    internal class LootProp
    {
        public Item Item;
        public McsAIBossPlayer McsAIBossPlayer;
        public TraderOffer Offer;
        public bool IsHighPriceItem = false;
        public bool IsWishListItem = false;
        public bool IsQuestNeedItem = false;
        public bool IsBlockItem = false;
        

        public LootProp(Item item, TraderOffer offer, McsAIBossPlayer mcsAIBossPlayer)
        {
            Item = item;
            Offer = offer;
            McsAIBossPlayer = mcsAIBossPlayer;
        }

        public void CheckHighPriceItem()
        {
            IsHighPriceItem = Offer.Price >= McsAIBossPlayer.McsBotPlayerConfig.PriceThreshold;
        }

        public void CheckWishListItem()
        {
            var wishlistManager = McsAIBossPlayer.Player().Profile.WishlistManager;
            if (wishlistManager != null)
            {
                IsWishListItem = wishlistManager.IsInWishlist(Item.TemplateId, true, out _);
            }
            IsWishListItem = false;
        }

        public void CheckBlockItem(EItemType itemType)
        {
            IsBlockItem = Tools.IsBlockItem((EBlockItemType)McsAIBossPlayer.McsBotPlayerConfig.BlockItemType, itemType);
        }

        // public bool CheckQuestNeedItem()
        // {

        // }
    }
}