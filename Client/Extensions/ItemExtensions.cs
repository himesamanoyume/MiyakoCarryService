using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    internal static class ItemExtensions
    {
        private static readonly ConditionalWeakTable<Item, ItemData> _datas = new();

        private static SquadMgr SquadMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        extension(Item item)
        {
            public ItemData GetData()
            {
                return _datas.TryGetValue(item, out ItemData itemData) ? itemData : item.InitData();
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

            private ItemData InitData()
            {
                if (item.IsPlayerInventory)
                {
                    var gameWorld = Singleton<GameWorld>.Instance;
                    var player = item.Owner switch
                    {
                        CorpseTraderControllerClass corpseTraderControllerClass => gameWorld.GetEverExistedPlayerByID(corpseTraderControllerClass.KilledProfileID),
                        _ => gameWorld.GetEverExistedPlayerByID(item.Owner.ID)
                    };

                    if (player == null)
                    {
                        MiyakoCarryServicePlugin.Logger.LogError("player 为空");
                        return null;
                    }

                    PlayerData playerData;
                    if (player.IsAI)
                    {
                        if (SquadMgr.IsMcsBotPlayer(player.ProfileId))
                        {
                            var mcsBossPlayer = SquadMgr.GetMcsBossPlayerByMcsBotPlayerId(player.ProfileId);
                            playerData = new McsBotPlayerData(SquadMgr.GetMcsBossPlayerByMcsBotPlayerId(player.ProfileId), SquadMgr.GetMcsAIBossPlayerByMcsBossId(mcsBossPlayer.ProfileId), player, item);
                            _datas.Add(item, playerData);
                            return playerData;
                        }
                    }
                    playerData = new PlayerData(player, item);
                    _datas.Add(item, playerData);
                    return playerData;
                }

                var lootData = new LootData(item, ContainsBestPrice(item));
                var mcsAIBossPlayers = SquadMgr.GetAllMcsAIBossPlayer();
                foreach (var mcsAIBossPlayer in mcsAIBossPlayers)
                {
                    lootData.Refresh(mcsAIBossPlayer);
                }
                _datas.Add(item, lootData);
                return lootData;
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
                    return price.HasValue ? new TraderOffer
                    (
                        price.Value.Amount,
                        item.Width * item.Height,
                        GetCurrencyType(TraderUtilsClass.GetCurrencyCharById(price.Value.CurrencyId.Value)),
                        trader.LocalizedName
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
    }
}