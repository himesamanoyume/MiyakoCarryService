using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Servers.Ws;
using SPTarkov.Server.Core.Utils.Cloners;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public class RaidService(
        NotificationHelper notificationHelper,
        NotificationSendHelper notificationSendHelper,
        SptWebSocketConnectionHandler sptWebSocketConnectionHandler,
        CompatibilityService compatibilityService,
        IServiceProvider serviceProvider,
        ProfileHelper profileHelper,
        InfoService infoService,
        ProfileService profileService,
        ICloner cloner
    )
    {
        private readonly ConcurrentDictionary<MongoId, List<int>> _leadMemberGroups = new();

        // 预留给以后增援护航的
        private readonly ConcurrentDictionary<MongoId, List<int>> _leadHelperMemberGroups = new();
        // end

        private readonly ConcurrentDictionary<MongoId, HashSet<MongoId>> _matchLeaders = new();
        private readonly Dictionary<MongoId, McsBotPlayerConfigRequestData> _mcsBotPlayerConfigs = new();
        private SemaphoreSlim _saveLock = new(1, 1);

        public async Task OnPostLoadAsync()
        {

        }

        public bool CheckMcsBotPlayerExist(MongoId mcsLeadPlayerId, int mcsAid)
        {
            if (_saveLock is null)
            {
                _saveLock = new(1, 1);
            }

            _saveLock.Wait();

            try
            {
                if (_leadMemberGroups.TryGetValue(mcsLeadPlayerId, out var mcsAids))
                {
                    if (mcsAids.Contains(mcsAid))
                    {
                        return true;
                    }
                }
                if (_leadHelperMemberGroups.TryGetValue(mcsLeadPlayerId, out mcsAids))
                {
                    if (mcsAids.Contains(mcsAid))
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public IEnumerable<MongoId> GetMySquadMcsBotPlayerIds(MongoId mcsLeadPlayerId, SideType side)
        {
            if (_saveLock is null)
            {
                _saveLock = new(1, 1);
            }

            _saveLock.Wait();

            try
            {
                _leadMemberGroups.TryGetValue(mcsLeadPlayerId, out var mcsAids);
                if (mcsAids == null)
                {
                    yield return new();
                }

                foreach (var mcsAid in mcsAids)
                {
                    var profile = profileService.GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, mcsAid);
                    if (profile == null)
                    {
                        continue;
                    }

                    yield return side == SideType.Pmc ? profile.ProfileInfo.ProfileId.Value : profile.ProfileInfo.ScavengerId.Value;
                }
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public void AddGroupMember(MongoId mcsLeadPlayerId, int mcsAid)
        {
            if (_saveLock is null)
            {
                _saveLock = new(1, 1);
            }

            _saveLock.Wait();

            try
            {
                var mcsAids = _leadMemberGroups.GetOrAdd(mcsLeadPlayerId, _ => new List<int>());
                if (!mcsAids.Contains(mcsAid))
                {
                    if (mcsAids.Count >= 4)
                    {
                        mcsAids.Clear();
                    }
                    mcsAids.Add(mcsAid);
                }
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public void RemoveGroupMember(MongoId mcsLeadPlayerId, int mcsAid)
        {
            if (_saveLock is null)
            {
                _saveLock = new(1, 1);
            }

            _saveLock.Wait();

            try
            {
                var mcsAids = _leadMemberGroups.GetOrAdd(mcsLeadPlayerId, _ => new List<int>());
                if (mcsAids.Contains(mcsAid))
                {
                    mcsAids.Remove(mcsAid);
                }
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public void AddMatchPlayer(MongoId mcsLeadPlayerId, MongoId otherPlayerId)
        {
            var matchPlayerIds = _matchLeaders.GetOrAdd(mcsLeadPlayerId, _ => new());
            matchPlayerIds.Add(otherPlayerId);
        }

        public void ClearGroupMember(MongoId mcsLeadPlayerId)
        {
            if (_saveLock is null)
            {
                _saveLock = new(1, 1);
            }

            _saveLock.Wait();

            try
            {
                _leadMemberGroups.GetOrAdd(mcsLeadPlayerId, _ => new()).Clear();
                _leadHelperMemberGroups.GetOrAdd(mcsLeadPlayerId, _ => new()).Clear();
                _matchLeaders.TryRemove(mcsLeadPlayerId, out _);
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public async Task AcceptGroupInvite(MongoId mcsLeadPlayerId, int mcsAid)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            var mcsBotPlayerFullProfile = profileService.GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, mcsAid);

            if (mcsBotPlayerFullProfile is null)
            {
                return;
            }

            if (CheckMcsBotPlayerExist(mcsLeadPlayerId, mcsAid) || infoService.IsOrderExpiredByBotPlayerProfileId(mcsBotPlayerFullProfile.ProfileInfo.ProfileId.Value))
            {
                try
                {
                    if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsLeadPlayerId))
                    {
                        var notification = notificationHelper.GenerateWsGroupMatchInviteDecline(mcsBotPlayerFullProfile);
                        await notificationSendHelper.SendMessageAsync(mcsLeadPlayerId, notification);
                    }
                }
                finally
                {

                }
            }
            else
            {
                try
                {
                    if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsLeadPlayerId))
                    {
                        var notification = notificationHelper.GenerateWsGroupMatchInviteAccept(mcsBotPlayerFullProfile);
                        await notificationSendHelper.SendMessageAsync(mcsLeadPlayerId, notification);
                    }
                }
                finally
                {
                    AddGroupMember(mcsLeadPlayerId, mcsAid);
                }
            }
        }

        public List<SptProfile> GetAllGroupMemberProfiles(MongoId mcsLeadPlayerId)
        {
            if (_saveLock is null)
            {
                _saveLock = new(1, 1);
            }

            _saveLock.Wait();

            try
            {
                var members = _leadMemberGroups.GetOrAdd(mcsLeadPlayerId, _ => new List<int>());
                var profiles = new List<SptProfile>();
                foreach (var mcsAid in members)
                {
                    var profile = profileService.GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, mcsAid);
                    if (profile is not null)
                    {
                        profiles.Add(profile);
                    }
                }
                return profiles;
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public async Task<List<MongoId>> GetAllMcsBotPlayerIdInRaid(MongoId mcsLeadPlayerId, SideType side)
        {
            var mcsLeadPlayerIds = GetAllMcsLeadPlayerIds(mcsLeadPlayerId);

            var tasks = mcsLeadPlayerIds.Select(async mcsLeadPlayerId =>
            {
                var isPmc = side is SideType.Pmc;
                var profileIds = await Task.Run(() =>
                {
                    var profiles = GetAllGroupMemberProfiles(mcsLeadPlayerId);
                    return profiles.Select(p => isPmc ? p.ProfileInfo.ProfileId.Value : p.ProfileInfo.ScavengerId.Value).ToList();
                });

                var mcsLeadPlayerProfile = profileHelper.GetFullProfile(mcsLeadPlayerId);
                return profileIds;
            });

            var results = await Task.WhenAll(tasks);

            var mcsBotPlayerIdInRaids = results.SelectMany(list => list).ToList();

            return mcsBotPlayerIdInRaids;
        }

        public HashSet<MongoId> GetAllMcsLeadPlayerIds(MongoId mcsLeadPlayerId)
        {
            var mcsLeadPlayerIds = new HashSet<MongoId> { mcsLeadPlayerId };
            if (compatibilityService.HasFikaServer)
            {
                var fikaMatchServiceType = compatibilityService.FikaMatchServiceType;
                var fikaMatchService = serviceProvider.GetService(fikaMatchServiceType);
                var matchId = (MongoId?)AccessTools.Method(fikaMatchServiceType, "GetMatchIdByPlayer").Invoke(fikaMatchService, [mcsLeadPlayerId]);

                if (matchId is not null)
                {
                    var fikaMatch = AccessTools.Method(fikaMatchServiceType, "GetMatch").Invoke(fikaMatchService, [matchId]);

                    if (fikaMatch is not null)
                    {
                        var fikaPlayers = AccessTools.Property(compatibilityService.FikaMatchType, "Players").GetValue(fikaMatch);
                        var fikaPlayerIds = (System.Collections.IEnumerable)fikaPlayers.GetType().GetProperty("Keys").GetValue(fikaPlayers);

                        foreach (MongoId playerId in fikaPlayerIds)
                        {
                            if (playerId != mcsLeadPlayerId)
                            {
                                AddMatchPlayer(mcsLeadPlayerId, playerId);
                                mcsLeadPlayerIds.Add(playerId);
                            }
                        }
                    }
                }
            }

            return mcsLeadPlayerIds;
        }

        public async Task<Dictionary<MongoId, IEnumerable<PmcData>>> SpawnMcsBotPlayer(MongoId mcsLeadPlayerId, SideType side)
        {
            var mcsLeadPlayerIds = GetAllMcsLeadPlayerIds(mcsLeadPlayerId);

            var tasks = mcsLeadPlayerIds.Select(async mcsLeadPlayerId =>
            {
                var isPmc = side is SideType.Pmc;
                var pmcDatas = await Task.Run(() =>
                {
                    var profiles = GetAllGroupMemberProfiles(mcsLeadPlayerId);
                    return profiles.Select(p => isPmc ? p.CharacterData.PmcData : p.CharacterData.ScavData)
                                .Select(CloneAndRemapInventoryIds)
                                .ToList();
                });

                var mcsLeadPlayerProfile = profileHelper.GetFullProfile(mcsLeadPlayerId);

                return new KeyValuePair<MongoId, IEnumerable<PmcData>>(isPmc ? mcsLeadPlayerProfile.ProfileInfo.ProfileId.Value : mcsLeadPlayerProfile.ProfileInfo.ScavengerId.Value, pmcDatas);
            });

            var results = await Task.WhenAll(tasks);

            var mcsPmcDatas = results.ToDictionary(
                pair => pair.Key,
                pair => pair.Value
            );

            return mcsPmcDatas;
        }

        public PmcData CloneAndRemapInventoryIds(PmcData originalPmcData)
        {
            var clonePmcData = cloner.Clone(originalPmcData);

            var inventory = clonePmcData?.Inventory;
            if (inventory?.Items is null || inventory.Items.Count == 0)
            {
                return clonePmcData;
            }

            var idMap = new Dictionary<MongoId, MongoId>();
            foreach (var item in inventory.Items)
            {
                idMap[item.Id] = new MongoId();
            }

            foreach (var item in inventory.Items)
            {
                if (idMap.TryGetValue(item.Id, out var newId))
                {
                    item.Id = newId;
                }

                if (!string.IsNullOrEmpty(item.ParentId) && idMap.TryGetValue(item.ParentId, out var newParentId))
                {
                    item.ParentId = newParentId;
                }
            }

            inventory.Equipment = Remap(idMap, inventory.Equipment);
            inventory.Stash = Remap(idMap, inventory.Stash);
            inventory.QuestRaidItems = Remap(idMap, inventory.QuestRaidItems);
            inventory.QuestStashItems = Remap(idMap, inventory.QuestStashItems);
            inventory.SortingTable = Remap(idMap, inventory.SortingTable);
            inventory.HideoutCustomizationStashId = Remap(idMap, inventory.HideoutCustomizationStashId);

            if (inventory.FastPanel is not null)
            {
                foreach (var key in inventory.FastPanel.Keys.ToList())
                {
                    inventory.FastPanel[key] = Remap(idMap, inventory.FastPanel[key]);
                }
            }

            if (inventory.HideoutAreaStashes is not null)
            {
                foreach (var key in inventory.HideoutAreaStashes.Keys.ToList())
                {
                    inventory.HideoutAreaStashes[key] = Remap(idMap, inventory.HideoutAreaStashes[key]);
                }
            }

            if (inventory.FavoriteItems is not null)
            {
                inventory.FavoriteItems = inventory.FavoriteItems.Select(favoriteItem => Remap(idMap, favoriteItem)).ToList();
            }

            return clonePmcData;
        }

        public MongoId Remap(Dictionary<MongoId, MongoId> idMap, MongoId id)
        {
            return idMap.TryGetValue(id, out var newId) ? newId : id;
        }

        public MongoId? Remap(Dictionary<MongoId, MongoId> idMap, MongoId? id)
        {
            if (id is null)
            {
                return null;
            }

            return idMap.TryGetValue(id.Value, out var newId) ? newId : id;
        }

        public async Task<Dictionary<MongoId, McsBotPlayerConfigRequestData>> GetMcsBotPlayerConfigs(MongoId mcsLeadPlayerId)
        {
            var mcsLeadPlayerIds = GetAllMcsLeadPlayerIds(mcsLeadPlayerId);

            var tasks = mcsLeadPlayerIds.Select(async mcsLeadPlayerId =>
            {
                var mcsBotPlayerConfig = await Task.Run(() =>
                {
                    if (_mcsBotPlayerConfigs.TryGetValue(mcsLeadPlayerId, out var mcsBotPlayerConfig))
                    {
                        return mcsBotPlayerConfig;
                    }
                    else
                    {
                        return new McsBotPlayerConfigRequestData
                        {
                            McsLeadPlayerId = mcsLeadPlayerId,
                            EnableLooting = false,
                            PriceThreshold = 50000,
                            KeywordItemText = "",
                            LootingKeywordItem = true,
                            BlockItemType = 0,
                            EnableKeepFormation = false,
                            FormationMatrix = "0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,5,6,0,7,8,0,0,0,0,-1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0",
                            FormationSpacing = 3f,
                            FormationSequentialFill = false,
                            PhrasesSilent = false
                        };
                    }
                });

                return new KeyValuePair<MongoId, McsBotPlayerConfigRequestData>(mcsLeadPlayerId, mcsBotPlayerConfig);
            });

            var results = await Task.WhenAll(tasks);
            var mcsBotPlayerConfigs = results.ToDictionary(
                pair => pair.Key,
                pair => pair.Value
            );
            return mcsBotPlayerConfigs;
        }

        public async Task CollectMcsBotPlayerConfig(McsBotPlayerConfigRequestData mcsBotPlayerConfig)
        {
            _mcsBotPlayerConfigs[mcsBotPlayerConfig.McsLeadPlayerId] = mcsBotPlayerConfig;
        }
    }
}