using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Trading;
using EFT.UI.DragAndDrop;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Patches.Events;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class ItemExtensions
    {

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        extension(Item item)
        {
            public ItemData GetData()
            {
                return InteropDataCache<ItemData>.GetOrCreate(item.Pointer, () => item.InitData(), cacheNegative: false);
            }

            public IEnumerable<ItemData> GetAllDatas()
            {
                foreach (var subItem in item.GetAllItems())
                {
                    ItemData data = GetData(subItem);
                    if (data != null)
                    {
                        yield return data;
                    }
                }
            }

            public bool McsRemoveItem()
            {
                try
                {
                    var iItemOwner = item.Parent.GetOwner();
                    var traderControllerClass = iItemOwner == null ? null : iItemOwner.TryCast<ItemController>();
                    if (traderControllerClass == null)
                    {
                        return false;
                    }

                    var result = ItemManipulator.Discard(item, traderControllerClass, false);
                    if (result.Error != null)
                    {
                        return false;
                    }

                    result.Value.RaiseEvents(traderControllerClass, CommandStatus.Begin);
                    result.Value.RaiseEvents(traderControllerClass, CommandStatus.Succeed);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            private ItemData InitData()
            {
                if (item.IsPlayerInventory)
                {
                    var gameWorld = Singleton<GameWorld>.Instance;

                    var profileId = item.Owner switch
                    {
                        CorpseItemController c => c?.KilledProfileID,
                        _ => item.Owner?.ID
                    };

                    var player = !string.IsNullOrEmpty(profileId) ? gameWorld.GetEverExistedPlayerByProfileId(profileId) : null;

                    if (player == null)
                    {
                        return null;
                    }

                    PlayerData playerData;
                    if (player.IsAI)
                    {
                        if (McsMgr.IsMcsBotPlayer(player.ProfileId))
                        {
                            var McsLeadPlayer = McsMgr.GetMcsLeadPlayerByMcsBotPlayerId(player.ProfileId);
                            playerData = new McsBotPlayerData(McsMgr.GetMcsLeadPlayerByMcsBotPlayerId(player.ProfileId), McsMgr.GetMcsAILeadPlayerByMcsLeadPlayerId(McsLeadPlayer.ProfileId), player, item);
                            return playerData;
                        }
                    }
                    playerData = new PlayerData(player, item);
                    return playerData;
                }

                var lootData = new LootData(item, ContainsBestPrice(item));
                return lootData;
            }

            public bool IsPlayerInventory => item.StringTemplateId == ItemTpl.DefaultInventory;

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
                    var itemType = ItemViewFactory.GetItemType(ManagedArguments.ToIl2CppType(item.GetType()));
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
                                    if (offer == null && !item.IsMoney())
                                    {
                                        offer = new TraderOffer();
                                    }

                                    if (offer != null)
                                    {
                                        gameloop.ItemBestPriceDict[item.TemplateId] = offer;
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

                    if (trader._supplyData == null)
                    {
                        TraderClassConstructorPatch.RequestSupplyData(trader);
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

            public TraderOffer GetTraderOffer(Trader trader)
            {
                try
                {
                    Il2CppSystem.Nullable<Trader.ItemPrice> price = null;
                    try
                    {
                        price = trader.GetUserItemPrice(item);
                    }
                    catch
                    {
                    }

                    var gameAmount = price != null && price.HasValue ? price.Value.Amount : -1;
                    var amount = gameAmount > 0 ? gameAmount : CalculateFallbackAmount(item, trader);

                    if (amount <= 0)
                    {
                        return new TraderOffer();
                    }

                    return new TraderOffer(
                        amount,
                        item.Width * item.Height,
                        GetTraderCurrencyType(trader),
                        trader.LocalizedName
                    );
                }
                catch
                {
                    return new TraderOffer();
                }
            }

            private static long CalculateFallbackAmount(Item target, Trader trader)
            {
                try
                {
                    var supply = trader._supplyData;
                    if (supply == null)
                    {
                        return 0;
                    }

                    var basePrice = supply.GetBasePrice(target.TemplateId);
                    if (basePrice <= 0)
                    {
                        return 0;
                    }

                    double course = 1;
                    try
                    {
                        var currencyKey = (string)CurrencyUtil.GetCurrencyId(trader.Settings.Currency);
                        var courses = trader.CurrencyCourses;
                        if (courses != null && courses.ContainsKey(currencyKey))
                        {
                            var courseValue = courses[currencyKey];
                            if (courseValue > 0)
                            {
                                course = courseValue;
                            }
                        }
                    }
                    catch
                    {
                    }

                    var sellMod = trader.Info.SellToTraderPriceModifier;
                    var amount = Math.Floor(basePrice / course * sellMod);
                    return amount > 0 ? (long)amount : 0;
                }
                catch
                {
                    return 0;
                }
            }

            private static ECurrencyType GetTraderCurrencyType(Trader trader)
            {
                var currencyId = CurrencyUtil.GetCurrencyId(trader.Settings.Currency);
                return GetCurrencyType(CurrencyUtil.GetCurrencyCharById(currencyId));
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

            public bool IsMoney()
            {
                var templateId = item.StringTemplateId;
                if (templateId != null)
                {
                    return Classification.MoneyItems.Contains(templateId);
                }
                return false;
            }

            public IEnumerable<Item> GetAllChildItemsOnly()
            {
                var collection = item == null ? null : item.TryCast<ContainerCollection>();
                if (collection != null)
                {
                    foreach (var container in collection.Containers)
                    {
                        foreach (var child in container.Items)
                        {
                            foreach (var descendant in child.GetAllItems())
                            {
                                yield return descendant;
                            }
                        }
                    }
                }
            }

            public bool IncludeTargetItem(Item target)
            {
                var items = item.GetAllChildItemsOnly();
                foreach (var _item in items)
                {
                    if (_item.Id == target.Id)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public static void Clear()
        {
            InteropDataCache<ItemData>.Clear();
        }
    }
}