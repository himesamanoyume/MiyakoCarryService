using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Servers.Ws;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class RaidService(
        NotificationHelper notificationHelper,
        NotificationSendHelper notificationSendHelper,
        SptWebSocketConnectionHandler sptWebSocketConnectionHandler,
        CompatibilityService compatibilityService,
        ProfileHelper profileHelper,
        ProfileService profileService
    )
    {
        private readonly ConcurrentDictionary<MongoId, List<int>> _leadMemberGroups = new();
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
                return false;
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public IEnumerable<MongoId> GetMcsBotPlayerIds(MongoId mcsLeadPlayerId)
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

                    yield return profile.ProfileInfo.ProfileId.Value;
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
                // logger.Info($"正在清除 {mcsLeadPlayerId} 的小队成员");
                _leadMemberGroups.GetOrAdd(mcsLeadPlayerId, _ => new()).Clear();
                var matchPlayerIds = _matchLeaders.GetOrAdd(mcsLeadPlayerId, _ => new());
                foreach (var matchPlayerId in matchPlayerIds)
                {
                    // logger.Info($"正在清除房主 {mcsLeadPlayerId} 的联机队友 {matchPlayerId} 的小队成员");
                    _leadMemberGroups.GetOrAdd(matchPlayerId, _ => new()).Clear();
                }
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public void AcceptGroupInvite(MongoId mcsLeadPlayerId, int mcsAid)
        {
            var mcsBotPlayerFullProfile = profileService.GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, mcsAid);

            if (mcsBotPlayerFullProfile is null)
            {
                return;
            }

            if (CheckMcsBotPlayerExist(mcsLeadPlayerId, mcsAid))
            {
                try
                {
                    if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsLeadPlayerId))
                    {
                        var notification = notificationHelper.GenerateWsGroupMatchInviteDecline(mcsBotPlayerFullProfile);
                        notificationSendHelper.SendMessage(mcsLeadPlayerId, notification);
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
                        // var notification2 = notificationHelper.GenerateWsGroupMatchRaidReady(mcsBotPlayerFullProfile);
                        notificationSendHelper.SendMessage(mcsLeadPlayerId, notification);
                        // notificationSendHelper.SendMessage(mcsLeadPlayerId, notification2);
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

        public async Task<List<MongoId>> GetAllMcsBotPlayerIdInRaid(MongoId mcsLeadPlayerId)
        {
            var mcsLeadPlayerIds = GetAllMcsLeadPlayerIds(mcsLeadPlayerId);

            var tasks = mcsLeadPlayerIds.Select(async mcsLeadPlayerId =>
            {
                var profileIds = await Task.Run(() =>
                {
                    var profiles = GetAllGroupMemberProfiles(mcsLeadPlayerId);
                    return profiles.Select(p => p.ProfileInfo.ProfileId.Value).ToList();
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
                var fikaMatchService = ServiceLocator.ServiceProvider.GetService(fikaMatchServiceType);
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
                    return profiles.Select(p => isPmc ? p.CharacterData.PmcData : p.CharacterData.ScavData).ToList();
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
                            PriceThreshold = 50000,
                            ArmorLevelThreshold = 5,
                            LootingWishlishItem = true,
                            LootingQuestItem = true,
                            BlockItemType = 0
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