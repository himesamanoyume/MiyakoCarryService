

using System;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Datas
{
    public class LootProp
    {
        public Item Item;
        public McsAILeadPlayer McsAILeadPlayer;
        public TraderOffer Offer;
        public bool IsHighPriceItem = false;
        public bool IsKeywordItem = false;
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

        public void CheckKeywordItem()
        {
            var keywordArray = McsAILeadPlayer.McsBotPlayerConfig.KeywordItemText;
            foreach (var keyword in keywordArray.Split(["||", ",", "，"], StringSplitOptions.RemoveEmptyEntries))
            {
                if (Item.ShortName.McsLocalized().Contains(keyword) || Item.Name.McsLocalized().Contains(keyword))
                {
                    IsKeywordItem = true;
                    return;
                }
            }
            IsKeywordItem = false;
        }

        public void CheckBlockItem(EItemType itemType)
        {
            IsBlockItem = Tools.IsBlockItem((EBlockItemType)McsAILeadPlayer.McsBotPlayerConfig.BlockItemType, itemType);
        }
    }
}