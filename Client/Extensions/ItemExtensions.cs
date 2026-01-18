using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    internal static class ItemExtensions
    {
        private static readonly ConditionalWeakTable<Item, ItemData> _datas = new();

        extension(Item item)
        {
            public ItemData GetData()
            {
                return _datas.GetValue(item, InitData);
            }

            public bool IsPlayerInventory => item.StringTemplateId == CommonId.DefaultInventory;

            public TraderOffer ContainsBestPrice()
            {
                TraderOffer offer;
                var gameloop = GameLoop.Instance;
                if (string.IsNullOrEmpty(item.StringTemplateId))
                {
                    return new TraderOffer();
                }
                else
                {
                    var itemType = ItemViewFactory.GetItemType(item.GetType());
                    switch (itemType)
                    {
                        case EItemType.Armor:
                        case EItemType.Ammo:
                        case EItemType.Weapon:
                        case EItemType.Magazine:
                            {
                                offer = GetBestTraderOffer(item);
                                return offer == null ? new TraderOffer() : offer;
                            }
                        default:
                            {
                                if (!gameloop.ItemBestPriceDict.TryGetValue(item.TemplateId, out offer))
                                {
                                    offer = GetBestTraderOffer(item);
                                    if (offer != null)
                                    {
                                        gameloop.ItemBestPriceDict.Add(item.TemplateId, offer);
                                    }
                                    else
                                    {
                                        if (!item.IsMoney())
                                        {
                                            gameloop.ItemBestPriceDict.Add(item.TemplateId, new TraderOffer());
                                        }
                                    }
                                }
                                return offer;
                            }
                    }
                }
            }

            public TraderOffer GetBestTraderOffer()
            {
                foreach (var offer in item.GetAllTraderOffers())
                {
                    return offer;
                }
                return null;
            }

            public IEnumerable<TraderOffer> GetAllTraderOffers()
            {
                if (item.Owner?.OwnerType is EOwnerType.RagFair || item.Owner?.OwnerType is EOwnerType.Trader
                    && (item.StackObjectsCount > 1 || item.UnlimitedCount))
                {
                    item = item.CloneItem();
                    item.StackObjectsCount = 1;
                    item.UnlimitedCount = false;
                }

                var offers = new List<TraderOffer>();
                foreach (var trader in GameLoop.Instance.Session.Traders)
                {
                    if (trader.Settings.AvailableInRaid)
                    {
                        continue;
                    }

                    var offer = item.GetTraderOffer(trader);
                    if (offer != null)
                    {
                        offers.Add(offer);
                    }
                }

                offers.Sort((a, b) => b.Price.CompareTo(a.Price));
                return offers;
            }

            public TraderOffer GetTraderOffer(TraderClass trader)
            {
                try
                {
                    var price = trader.GetUserItemPrice(item);
                    return price.HasValue ? new TraderOffer(
                        trader.LocalizedName,
                        price.Value.Amount,
                        item.Width * item.Height,
                        GetCurrencyType(TraderUtilsClass.GetCurrencyCharById(price.Value.CurrencyId.Value))
                    ) : new TraderOffer();

                }
                catch
                {
                    return new TraderOffer();
                }
            }

            public bool IsMoney()
            {
                var templateId = item.StringTemplateId;
                if (templateId != null)
                {
                    return Classification.MoneyItems.Contains(templateId);
                }
                return false;
            }
        }

        private static ECurrencyType GetCurrencyType(string currency)
        {
            return currency switch
            {
                "€" => ECurrencyType.EUR,
                "$" => ECurrencyType.USD,
                "<sprite=0>" or "GP" => ECurrencyType.GP,
                _ => ECurrencyType.RUB
            };
        }

        private static ItemData InitData(Item target)
        {
            if (target.IsPlayerInventory)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                var player = target.Owner switch
                {
                    CorpseTraderControllerClass corpseTraderControllerClass => gameWorld.GetEverExistedPlayerByID(corpseTraderControllerClass.KilledProfileID),
                    _ => gameWorld.GetEverExistedPlayerByID(target.Owner.ID)
                };

                if (player == null)
                {
                    return null;
                }

                var playerData = new PlayerData(player, target);
                _datas.Add(target, playerData);
                return playerData;
            }

            var lootData = new LootData(target, ContainsBestPrice(target));
            lootData.Reset();
            _datas.Add(target, lootData);
            return lootData;
        }
    }
}