

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
        ItemHelper itemHelper,
        MailSendService mailSendService,
        ConfigService configService
    )
    {
        private readonly string _traderFolderDir = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "traders", MiyakoTraderId);
        public const string MiyakoTraderId = "6952ced4bcc1dd1e3c80dfcb";

        // 因为SPT会检查行动任务的商人Id是否存在，为了防止频繁提示存档被标记为不合法，因此创建任务时临时使用此商人Id
        public const string TempOrderTraderId = "6864e812f9fe664cb8b8e152";
        public const int TicketPricePerPercent = 300000;
        private Punish _punishmentMulti;
        private SemaphoreSlim _saveLock = new(1, 1);

        private readonly List<Item> _mcsBotPlayerInventoryModeItems = new();
        private readonly Dictionary<MongoId, List<List<BarterScheme>>> _mcsBotPlayerInventoryModeBarterScheme = new();
        private readonly Dictionary<MongoId, int>_mcsBotPlayerInventoryModeLoyalLevelItems = new();

        public async Task OnPostLoadAsync()
        {
            await GenerateMcsBotPlayerInventoryModeAssort();
            await LoadTrader();
            await LoadPunish();
        }

        private async Task LoadTrader()
        {
            var iconPath = System.IO.Path.Join(_traderFolderDir, $"{MiyakoTraderId}.jpg");
            var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(_traderFolderDir, "base.json");
            imageRouter.AddRoute(traderBase.Avatar.Replace(".jpg", ""), iconPath);
            AddTraderWithEmptyAssortToDb(traderBase);
            SetTraderUpdateTime(configServer.GetConfig<TraderConfig>(), traderBase, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
            var ragfairConfig = configServer.GetConfig<RagfairConfig>();
            if (!ragfairConfig.Traders.ContainsKey(MiyakoTraderId))
            {
                ragfairConfig.Traders[MiyakoTraderId] = true;
            }
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
            else if (_punishmentMulti.PunishmentMulti > 1d)
            {
                _punishmentMulti.PunishmentMulti = 1d;
                _ = SavePunishmentMulti();
            }
        }

        public double GetGlobalPunishmentMulti()
        {
            return _punishmentMulti.PunishmentMulti;
        }

        private void AddTraderWithEmptyAssortToDb(TraderBase traderDetailsToAdd)
        {
            var traderAssort = new TraderAssort
            {
                Items = new(),
                BarterScheme = new(),
                LoyalLevelItems = new()
            };

            var traderDataToAdd = new Trader
            {
                Assort = traderAssort,
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
                    StackObjectsCount = 9999999,
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
                    Count = 1,
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

        public void ModifyPunishmentMulti(double diff, bool isIncrease = true)
        {
            _punishmentMulti.PunishmentMulti = Math.Round(_punishmentMulti.PunishmentMulti + (isIncrease ? diff : -diff), 4);
            if (_punishmentMulti.PunishmentMulti < 0d)
            {
                _punishmentMulti.PunishmentMulti = 0d;
            }
            else if (_punishmentMulti.PunishmentMulti > 1d)
            {
                _punishmentMulti.PunishmentMulti = 1d;
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

            ModifyPunishmentMulti(info.Diff, true);
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