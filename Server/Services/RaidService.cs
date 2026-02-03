using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers.Ws;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class RaidService(
        NotificationHelper notificationHelper,
        NotificationSendHelper notificationSendHelper,
        SptWebSocketConnectionHandler sptWebSocketConnectionHandler,
        CompatibilityService compatibilityService,
        ProfileService profileService
    )
    {
        private readonly ConcurrentDictionary<MongoId, List<int>> _bossMemberGroups = new();
        private readonly Dictionary<MongoId, McsBotPlayerConfigRequestData> _mcsBotPlayerConfigs = new();

        public async Task OnPostLoadAsync()
        {

        }

        public bool CheckMcsBotPlayerExist(MongoId mcsBossPlayerId, int mcsAid)
        {
            if (_bossMemberGroups.TryGetValue(mcsBossPlayerId, out var mcsAids))
            {
                if (mcsAids.Contains(mcsAid))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddGroupMember(MongoId mcsBossPlayerId, int mcsAid)
        {
            var mcsAids = _bossMemberGroups.GetOrAdd(mcsBossPlayerId, _ => new List<int>());
            if (!mcsAids.Contains(mcsAid))
            {
                mcsAids.Add(mcsAid);
            }
        }

        public void RemoveGroupMember(MongoId mcsBossPlayerId, int mcsAid)
        {
            var mcsAids = _bossMemberGroups.GetOrAdd(mcsBossPlayerId, _ => new List<int>());
            if (mcsAids.Contains(mcsAid))
            {
                mcsAids.Remove(mcsAid);
            }
        }

        public void ClearGroupMember(MongoId mcsBossPlayerId)
        {
            _bossMemberGroups.GetOrAdd(mcsBossPlayerId, _ => new List<int>()).Clear();
        }

        public void AcceptGroupInvite(MongoId mcsBossPlayerId, int mcsAid)
        {
            var mcsBotPlayerFullProfile = profileService.GetMcsBotPlayerProfileByAccountId(mcsBossPlayerId, mcsAid);

            if (mcsBotPlayerFullProfile is null)
            {
                return;
            }

            if (CheckMcsBotPlayerExist(mcsBossPlayerId, mcsAid))
            {
                try
                {
                    if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsBossPlayerId))
                    {
                        var notification = notificationHelper.GenerateWsGroupMatchInviteDecline(mcsBotPlayerFullProfile);
                        notificationSendHelper.SendMessage(mcsBossPlayerId, notification);
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
                    if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsBossPlayerId))
                    {
                        var notification = notificationHelper.GenerateWsGroupMatchInviteAccept(mcsBotPlayerFullProfile);
                        // var notification2 = notificationHelper.GenerateWsGroupMatchRaidReady(mcsBotPlayerFullProfile);
                        notificationSendHelper.SendMessage(mcsBossPlayerId, notification);
                        // notificationSendHelper.SendMessage(mcsBossPlayerId, notification2);
                    }
                }
                finally
                {
                    AddGroupMember(mcsBossPlayerId, mcsAid);
                }
            }
        }

        public List<SptProfile> GetAllGroupMemberProfiles(MongoId mcsBossPlayerId)
        {
            var members = _bossMemberGroups.GetOrAdd(mcsBossPlayerId, _ => new List<int>());
            var profiles = new List<SptProfile>();
            foreach (var mcsAid in members)
            {
                var profile = profileService.GetMcsBotPlayerProfileByAccountId(mcsBossPlayerId, mcsAid);
                if (profile is not null)
                {
                    profiles.Add(profile);
                }
            }
            return profiles;
        }

        public async Task<Dictionary<MongoId, IEnumerable<PmcData>>> SpawnMcsBotPlayer(MongoId mcsBossPlayerId)
        {
            var mcsBossPlayerIds = new HashSet<MongoId> { mcsBossPlayerId };

            if (compatibilityService.HasFikaServer)
            {
                // 暂不进行验证
                var fikaMatchService = compatibilityService.FikaMatchService;
                var matchId = (MongoId?)AccessTools.Method(fikaMatchService, "GetMatchIdByPlayer")?.Invoke(null, [mcsBossPlayerId]);

                if (matchId is not null)
                {
                    var fikaMatch = AccessTools.Method(fikaMatchService, "GetMatch")?.Invoke(null, [matchId]);

                    if (fikaMatch is not null)
                    {
                        var fikaPlayers = (Dictionary<MongoId, object>)AccessTools.Property(compatibilityService.FikaMatch, "Players")?.GetValue(fikaMatch);

                        foreach (var playerId in fikaPlayers.Keys)
                        {
                            if (playerId != mcsBossPlayerId)
                            {
                                mcsBossPlayerIds.Add(playerId);
                            }
                        }
                    }
                }
            }

            var tasks = mcsBossPlayerIds.Select(async mcsBossPlayerId =>
            {
                var pmcDatas = await Task.Run(() =>
                {
                    var profiles = GetAllGroupMemberProfiles(mcsBossPlayerId);
                    // ClearGroupMember(mcsBossPlayerId);
                    return profiles.Select(p => p.CharacterData.PmcData).ToList();
                });

                return new KeyValuePair<MongoId, IEnumerable<PmcData>>(mcsBossPlayerId, pmcDatas);
            });

            var results = await Task.WhenAll(tasks);

            var mcsPmcDatas = results.ToDictionary(
                pair => pair.Key,
                pair => pair.Value
            );

            return mcsPmcDatas;
        }

        public async Task<Dictionary<MongoId, McsBotPlayerConfigRequestData>> GetMcsBotPlayerConfigs(MongoId mcsBossPlayerId)
        {
            var mcsBossPlayerIds = new HashSet<MongoId> { mcsBossPlayerId };

            if (compatibilityService.HasFikaServer)
            {
                // 暂不进行验证
                var fikaMatchService = compatibilityService.FikaMatchService;
                var matchId = (MongoId?)AccessTools.Method(fikaMatchService, "GetMatchIdByPlayer")?.Invoke(null, [mcsBossPlayerId]);

                if (matchId is not null)
                {
                    var fikaMatch = AccessTools.Method(fikaMatchService, "GetMatch")?.Invoke(null, [matchId]);

                    if (fikaMatch is not null)
                    {
                        var fikaPlayers = (Dictionary<MongoId, object>)AccessTools.Property(compatibilityService.FikaMatch, "Players")?.GetValue(fikaMatch);

                        foreach (var playerId in fikaPlayers.Keys)
                        {
                            if (playerId != mcsBossPlayerId)
                            {
                                mcsBossPlayerIds.Add(playerId);
                            }
                        }
                    }
                }
            }

            var tasks = mcsBossPlayerIds.Select(async mcsBossPlayerId =>
            {
                var mcsBotPlayerConfig = await Task.Run(() =>
                {
                    if (_mcsBotPlayerConfigs.TryGetValue(mcsBossPlayerId, out var mcsBotPlayerConfig))
                    {
                        return mcsBotPlayerConfig;
                    }
                    else
                    {
                        return new McsBotPlayerConfigRequestData
                        {
                            McsBossPlayerId = mcsBossPlayerId,
                            PriceThreshold = 50000,
                            ArmorLevelThreshold = 5,
                            LootingWishlishItem = true,
                            LootingQuestItem = true,
                            BlockItemType = 0
                        };
                    }
                });

                return new KeyValuePair<MongoId, McsBotPlayerConfigRequestData>(mcsBossPlayerId, mcsBotPlayerConfig);
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
            _mcsBotPlayerConfigs[mcsBotPlayerConfig.McsBossPlayerId] = mcsBotPlayerConfig;
        }
    }
}