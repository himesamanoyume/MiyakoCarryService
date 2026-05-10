

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Models.Eft.Trader;
using MiyakoCarryService.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class TraderService(
        ModHelper modHelper,
        ICloner cloner,
        ImageRouter imageRouter,
        ConfigServer configServer,
        TimeUtil timeUtil,
        DatabaseService databaseService,
        SaveServer saveServer,
        ProfileService profileService,
        JsonUtil jsonUtil,
        CompatibilityService compatibilityService,
        FileUtil fileUtil,
        ISptLogger<TraderService> logger,
        ItemHelper itemHelper,
        MailSendService mailSendService,
        ConfigService configService
    )
    {
        private readonly string _traderFolderDir = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "traders", MiyakoTraderId);
        public const string MiyakoTraderId = "6952ced4bcc1dd1e3c80dfcb";

        // 因为SPT会检查行动任务的商人Id是否存在，为了防止频繁提示存档被标记为不合法，因此创建任务时临时使用此商人Id
        public const string TempOrderTraderId = "6864e812f9fe664cb8b8e152";
        private Punish _punishmentMulti;
        private SemaphoreSlim _saveLock = new(1, 1);

        private readonly List<Item> _mcsBotPlayerInventoryModeItems = new();
        private readonly Dictionary<MongoId, List<List<BarterScheme>>> _mcsBotPlayerInventoryModeBarterScheme = new();
        private readonly Dictionary<MongoId, int>_mcsBotPlayerInventoryModeLoyalLevelItems = new();

        public async Task OnPostLoadAsync()
        {
            await LoadTrader();
            await LoadPunish();
            await GenerateMcsBotPlayerInventoryModeAssort();
        }

        private Task LoadTrader()
        {
            var iconPath = System.IO.Path.Join(_traderFolderDir, "miyako_halo.jpg");
            var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(_traderFolderDir, "base.json");
            imageRouter.AddRoute(traderBase.Avatar.Replace(".jpg", ""), iconPath);
            AddTraderWithEmptyAssortToDb(traderBase);
            var assort = modHelper.GetJsonDataFromFile<TraderAssort>(_traderFolderDir, "assort.json");
            OverwriteTraderAssort(traderBase.Id, assort);
            SetTraderUpdateTime(configServer.GetConfig<TraderConfig>(), traderBase, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
            return Task.CompletedTask;
        }

        private async Task LoadPunish()
        {
            var punishPath = System.IO.Path.Combine(_traderFolderDir, "punish.json");
            if (!fileUtil.FileExists(punishPath))
            {
                await fileUtil.WriteFileAsync(punishPath, jsonUtil.Serialize(new Punish(){ PunishmentMulti = 0}));
            }
            _punishmentMulti = await jsonUtil.DeserializeFromFileAsync<Punish>(punishPath);
            if (_punishmentMulti.PunishmentMulti < 0d)
            {
                _punishmentMulti.PunishmentMulti = 0d;
                _ = SavePunishmentMulti();
            }
            else if (_punishmentMulti.PunishmentMulti > 5d)
            {
                _punishmentMulti.PunishmentMulti = 5d;
                _ = SavePunishmentMulti();
            }
        }

        public double GetGlobalPunishmentMulti()
        {
            return _punishmentMulti.PunishmentMulti;
        }

        private void AddTraderWithEmptyAssortToDb(TraderBase traderDetailsToAdd)
        {
            var emptyTraderItemAssortObject = new TraderAssort
            {
                Items = [],
                BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                LoyalLevelItems = new Dictionary<MongoId, int>()
            };

            var traderDataToAdd = new Trader
            {
                Assort = emptyTraderItemAssortObject,
                Base = cloner.Clone(traderDetailsToAdd),
                QuestAssort = new()
                {
                    { "Started", new() },
                    { "Success", new() },
                    { "Fail", new() }
                },
                Dialogue = []
            };

            if (databaseService.GetTables().Traders.TryAdd(traderDetailsToAdd.Id, traderDataToAdd))
            {
                
            }
        }

        private Task GenerateMcsBotPlayerInventoryModeAssort()
        {
            var items = databaseService.GetItems();
            var prices = databaseService.GetPrices();
            
            foreach (var kvp in items) 
            {
                var templateItem = kvp.Value;
                if (templateItem.Type != "Item")
                {
                    continue;
                }

                if (templateItem.Id == ItemTpl.FACECOVER_BALACLAVA_TEST || templateItem.Id == ItemTpl.FACECOVER_BALACLAVA_DEV)
                {
                    continue;
                }

                var upd = new Upd
                {
                    UnlimitedCount = true,
                    StackObjectsCount = 9999,
                    BuyRestrictionCurrent = 0
                };
                var item = new Item
                {
                    Id = new(),
                    Template = templateItem.Id,
                    ParentId = "hideout",
                    SlotId = "hideout",
                    Upd = upd
                };

                _mcsBotPlayerInventoryModeItems.Add(item);

                var barterScheme = new BarterScheme
                {
                    Count = prices.TryGetValue(templateItem.Id, out var price) ? price : 10000,
                    Template = ItemTpl.MONEY_ROUBLES
                };

                _mcsBotPlayerInventoryModeBarterScheme.Add(item.Id, [[barterScheme]]);
                _mcsBotPlayerInventoryModeLoyalLevelItems.Add(item.Id, 1);

                if (templateItem.Parent == BaseClasses.ARMOR || templateItem.Parent == BaseClasses.HEADWEAR || templateItem.Parent == BaseClasses.VEST)
                {
                    foreach (var slot in templateItem.Properties?.Slots)
                    {
                        if (slot is null)
                        {
                            continue;
                        }

                        var locked = slot.Properties?.Filters?.ElementAt(0)?.Locked;
                        if (locked is null)
                        {
                            continue;
                        }

                        if (locked == false)
                        {
                            continue;
                        }

                        var slotUpd = new Upd
                        {
                            StackObjectsCount = 1
                        };
                        var slotItem = new Item
                        {
                            Id = new(),
                            Template = slot.Properties.Filters.ElementAt(0).Plate.Value,
                            ParentId = item.Id,
                            SlotId = slot.Name,
                            Upd = slotUpd
                        };
                        _mcsBotPlayerInventoryModeItems.Add(slotItem);
                    }
                }
                else if (templateItem.Parent == BaseClasses.AMMO_BOX)
                {
                    var stackSlot = templateItem.Properties?.StackSlots?.ElementAt(0);
                    if (stackSlot is null)
                    {
                        continue;
                    }

                    var slotTemplateId = stackSlot.Properties?.Filters?.ElementAt(0)?.Filter?.ElementAt(0);
                    if (slotTemplateId is null)
                    {
                        continue;
                    }

                    var slotUpd = new Upd
                    {
                        StackObjectsCount = stackSlot.MaxCount
                    };
                    var slotItem = new Item
                    {
                        Id = new(),
                        Template = slotTemplateId.Value,
                        ParentId = item.Id,
                        SlotId = "cartridges",
                        Upd = slotUpd
                    };

                    _mcsBotPlayerInventoryModeItems.Add(slotItem);
                }
            }

            return Task.CompletedTask;
        }

        public TraderAssort GetMcsBotPlayerInventoryModeAssort()
        {
            return new TraderAssort
            {
                Items = _mcsBotPlayerInventoryModeItems,
                BarterScheme = _mcsBotPlayerInventoryModeBarterScheme,
                LoyalLevelItems = _mcsBotPlayerInventoryModeLoyalLevelItems
            };
        }

        private void OverwriteTraderAssort(string traderId, TraderAssort newAssorts)
        {
            if (!databaseService.GetTables().Traders.TryGetValue(traderId, out var traderToEdit))
            {
                return;
            }
            traderToEdit.Assort = newAssorts;
        }

        private void SetTraderUpdateTime(TraderConfig traderConfig, TraderBase baseJson, int refreshTimeSecondsMin, int refreshTimeSecondsMax)
        {
            var traderRefreshRecord = new UpdateTime
            {
                TraderId = baseJson.Id,
                Seconds = new MinMax<int>(refreshTimeSecondsMin, refreshTimeSecondsMax)
            };

            traderConfig.UpdateTime.Add(traderRefreshRecord);
        }

        private void AddPunishmentMulti(double diff)
        {
            _punishmentMulti.PunishmentMulti = Math.Round(_punishmentMulti.PunishmentMulti + diff, 4);
            if (_punishmentMulti.PunishmentMulti < 0d)
            {
                _punishmentMulti.PunishmentMulti = 0d;
            }
            else if (_punishmentMulti.PunishmentMulti > 5d)
            {
                _punishmentMulti.PunishmentMulti = 5d;
            }
            _ = SavePunishmentMulti();
        }

        private async Task SavePunishmentMulti()
        {
            if (_saveLock is null)
            {
                _saveLock = new(1, 1);
            }
            
            await _saveLock.WaitAsync();
            try
            {
                try
                {
                    var punishPath = System.IO.Path.Combine(_traderFolderDir, "punish.json");
                    var jsonPunish = jsonUtil.Serialize(_punishmentMulti, true);
                    await fileUtil.WriteFileAsync(punishPath, jsonPunish);
                }
                catch
                {
                    
                }
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public void FriendlyFirePenalty(MongoId playerId, FriendlyFirePenaltyRequestData info)
        {
            if (info.TeamKill)
            {
                var tkPunishPlayerIds = new HashSet<MongoId>() { info.FriendlyFirePlayerId, playerId };
                
                if (info.PunishEveryone && compatibilityService.HasFikaServer)
                {
                    logger.Info($"尝试惩罚 {info.FriendlyFirePlayerId} 但其并不是老板, 转而惩罚房间内全体玩家");
                    var fikaMatchServiceType = compatibilityService.FikaMatchServiceType;
                    var fikaMatchService = ServiceLocator.ServiceProvider.GetService(fikaMatchServiceType);
                    var matchId = (MongoId?)AccessTools.Method(fikaMatchServiceType, "GetMatchIdByPlayer").Invoke(fikaMatchService, [playerId]);

                    if (matchId is not null)
                    {
                        var fikaMatch = AccessTools.Method(fikaMatchServiceType, "GetMatch").Invoke(fikaMatchService, [matchId]);

                        if (fikaMatch is not null)
                        {
                            var fikaPlayers = AccessTools.Property(compatibilityService.FikaMatchType, "Players").GetValue(fikaMatch);
                            var fikaPlayerIds = (System.Collections.IEnumerable)fikaPlayers.GetType().GetProperty("Keys").GetValue(fikaPlayers);

                            foreach (MongoId fikaPlayerId in fikaPlayerIds)
                            {
                                tkPunishPlayerIds.Add(fikaPlayerId);
                            }
                        }
                    }
                }

                foreach (var tkPunishPlayerId in tkPunishPlayerIds)
                {
                    profileService.TeamKillPunish(tkPunishPlayerId);
                }
            }

            logger.Warning($"进行全局 {Math.Round(info.Diff * 100d, 4)}% 的涨价惩罚");
            AddPunishmentMulti(info.Diff);
        }

        public void Compensation(CompensationRequestData info)
        {
            var roubles = new Item  
            {  
                Id = new MongoId(),  
                Template = ItemTpl.MONEY_ROUBLES,  
                Upd = new Upd { StackObjectsCount = 300000 },  
            };  

            mailSendService.SendLocalisedNpcMessageToPlayer(
                info.McsLeadPlayerId,
                MiyakoTraderId,
                MessageType.MessageWithItems,
                Locales.MIYAKOTRADERCOMPENSATION,
                itemHelper.SplitStackIntoSeparateItems(roubles).SelectMany(x => x).ToList(),
                timeUtil.GetHoursAsSeconds(168)
            );
        }

        public async Task<ProfileChange> UpdateProfile(MongoId mcsLeadPlayerId)
        {
            PmcData targetPmcData;

            if (profileService.IsMcsBotPlayerInventoryMode(mcsLeadPlayerId))
            {
                targetPmcData = profileService.GetMcsBotPlayerProfileForInventoryMode(mcsLeadPlayerId)[0];
            }
            else
            {
                targetPmcData = saveServer.GetProfile(mcsLeadPlayerId).CharacterData.PmcData;
            }

            return new()
            {
                Id = mcsLeadPlayerId,
                Experience = targetPmcData.Info.Experience,
                TraderRelations = targetPmcData.TradersInfo.ToDictionary(
                    trader => trader.Key,
                    trader => new TraderData
                    {
                        SalesSum = trader.Value.SalesSum,  
                        Standing = trader.Value.Standing,  
                        Loyalty = trader.Value.LoyaltyLevel,  
                        Unlocked = trader.Value.Unlocked,  
                        Disabled = trader.Value.Disabled, 
                    }
                )
            };
        }
    }
}