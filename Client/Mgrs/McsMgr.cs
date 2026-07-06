
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Patches.Events;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public class McsMgr : BaseMgr
    {
        private ConcurrentDictionary<MongoID, ConcurrentDictionary<MongoID, Player>> _mcsSquadDict = new();
        private HashSet<MongoID> _mcsLeadPlayerIds = new();
        private HashSet<MongoID> _allMcsBotPlayerIdInRaid = new();
        private HashSet<MongoID> _mcsDeadBotPlayerIds = new();
        private ConcurrentDictionary<MongoID, McsAILeadPlayer> _mcsAILeadPlayers = new();
        public Dictionary<MongoID, Dictionary<MongoID, GroupPlayerViewModelClass>> McsTransitBotPlayers = new();
        private Debouncer<MongoID, FriendlyFirePenalty> _friendlyFireDebouncer;
        public ConcurrentDictionary<MongoID, McsBotPlayerConfig> McsLeadPlayerConfigs = new();

        public bool IsHost = false;

        public override void Start()
        {
            base.Start();
            EventMgr.Subscribe<ConfigEntrySettingChangedEvent>(UpdateMcsBotPlayerConfig, this);
        }

        public void AddMcsSquadMember(MongoID mcsLeadPlayerId, MongoID mcsBotPlayerId, McsAILeadPlayer mcsAILeadPlayer = null)
        {
            var squadMembers = _mcsSquadDict.GetOrAdd(mcsLeadPlayerId, _ => new());

            squadMembers.AddOrUpdate(
                mcsBotPlayerId,
                mcsBotPlayerId =>
                {
                    var mcsBotPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(mcsBotPlayerId);
                    if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost && mcsBotPlayer != null)
                    {
                        mcsBotPlayer.Profile.Info.GroupId = "Fika";
                        mcsBotPlayer.Profile.Info.TeamId = "Fika";
                    }
                    return mcsBotPlayer;
                },
                (mcsBotPlayerId, oldMcsBotPlayer) =>
                {
                    if (oldMcsBotPlayer != null)
                    {
                        return oldMcsBotPlayer;
                    }
                    var newMcsBotPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(mcsBotPlayerId);
                    if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost && newMcsBotPlayer != null)
                    {
                        newMcsBotPlayer.Profile.Info.GroupId = "Fika";
                        newMcsBotPlayer.Profile.Info.TeamId = "Fika";
                    }
                    return newMcsBotPlayer;
                }
            );

            _mcsLeadPlayerIds.Add(mcsLeadPlayerId);
            _allMcsBotPlayerIdInRaid.Add(mcsBotPlayerId);

            if (mcsAILeadPlayer != null)
            {
                _mcsAILeadPlayers.AddOrUpdate(
                    mcsLeadPlayerId,
                    mcsLeadPlayerId => mcsAILeadPlayer,
                    (mcsLeadPlayerId, oldMcsAILeadPlayer) => mcsAILeadPlayer
                );
            }
        }

        public void AddMcsSquadMemberToTransit(MongoID mcsLeadPlayerId, BotOwner botOwner)
        {
            if (!McsTransitBotPlayers.TryGetValue(mcsLeadPlayerId, out var transitMembers))
            {
                transitMembers = new();
                McsTransitBotPlayers.Add(mcsLeadPlayerId, transitMembers);
            }

            if (!transitMembers.TryGetValue(botOwner.ProfileId, out var groupPlayerViewModelClass))
            {
                var info = botOwner.Profile.Info;
                groupPlayerViewModelClass = new GroupPlayerViewModelClass(new GroupPlayerDataClass
                {
                    AccountId = botOwner.AccountId,
                    Id = botOwner.ProfileId,
                    Info = new()
                    {
                        Level = info.Level,
                        PrestigeLevel = info.PrestigeLevel,
                        MemberCategory = info.MemberCategory,
                        SelectedMemberCategory = info.SelectedMemberCategory,
                        Nickname = info.Side == EPlayerSide.Savage ? info.MainProfileNickname : info.Nickname,
                        Side = info.Side,
                        SavageLockTime = info.SavageLockTime,
                        SavageNickname = info.Nickname,
                        GameVersion = info.GameVersion,
                        HasCoopExtension = info.HasCoopExtension,
                        Health = botOwner.Profile.Health
                    },
                    PlayerVisualRepresentation = new(new()
                    {
                        Level = info.Level,
                        MemberCategory = info.MemberCategory,
                        SelectedMemberCategory = info.SelectedMemberCategory,
                        Nickname = info.Side == EPlayerSide.Savage ? info.MainProfileNickname : info.Nickname,
                        Side = info.Side,
                        Health = botOwner.Profile.Health
                    }, botOwner.Profile.Customization, botOwner.Profile.Inventory.Equipment)
                });
                transitMembers.Add(botOwner.ProfileId, groupPlayerViewModelClass);
            }
        }

        public IEnumerable<Player> GetAllMcsSquadMembersByMcsLeadId(MongoID mcsLeadPlayerId)
        {
            var squadMembers = _mcsSquadDict.GetOrAdd(mcsLeadPlayerId, _ => new());
            foreach (var mcsBotPlayer in squadMembers.Values)
            {
                yield return mcsBotPlayer;
            }
        }

        public List<Player> GetAllMyMcsSquadMembers(out Player mcsLeadPlayer)
        {
            mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mcsLeadPlayer == null)
            {
                return null;
            }
            var squadMembers = _mcsSquadDict.GetOrAdd(mcsLeadPlayer.ProfileId, _ => new());
            return squadMembers.Values.ToList();
        }

        public IEnumerable<Player> GetAllAliveMcsSquadMembersByMcsLeadId(MongoID mcsLeadPlayerId)
        {
            var squadMembers = _mcsSquadDict.GetOrAdd(mcsLeadPlayerId, _ => new());
            foreach (var mcsBotPlayer in squadMembers.Values)
            {
                if (mcsBotPlayer.HealthController.IsAlive)
                {
                    yield return mcsBotPlayer;
                }
            }
        }

        public bool IsMcsBotPlayer(MongoID mcsBotPlayerId)
        {
            return _allMcsBotPlayerIdInRaid.Contains(mcsBotPlayerId);
        }

        public bool IsMyMcsBotPlayer(MongoID mcsLeadPlayerId, MongoID mcsBotPlayerId)
        {
            _mcsLeadPlayerIds.Add(mcsLeadPlayerId);
            _mcsSquadDict.TryGetValue(mcsLeadPlayerId, out var squadMembers);
            if (squadMembers != null)
            {
                return squadMembers.Keys.Contains(mcsBotPlayerId);
            }
            return false;
        }

        public bool IsMcsBotPlayerDead(MongoID mcsBotPlayerId)
        {
            return _mcsDeadBotPlayerIds.Contains(mcsBotPlayerId);
        }

        public void McsBotPlayerDead(MongoID mcsBotPlayerId)
        {
            _mcsDeadBotPlayerIds.Add(mcsBotPlayerId);
        }

        public bool IsMcsLeadPlayer(MongoID mcsLeadPlayerId)
        {
            return _mcsLeadPlayerIds.Contains(mcsLeadPlayerId);
        }

        public Player GetMcsLeadPlayerByMcsBotPlayerId(MongoID mcsBotPlayerId)
        {
            if (!IsHost)
            {
                throw new System.Exception("作为副机时不应使用此函数");
            }
            foreach (var mcsSquad in _mcsSquadDict)
            {
                if (mcsSquad.Value.ContainsKey(mcsBotPlayerId))
                {
                    return Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(mcsSquad.Key);
                }
            }
            return null;
        }

        public McsAILeadPlayer GetMcsAILeadPlayerByMcsLeadPlayerId(MongoID mcsLeadPlayerId)
        {
            if (_mcsAILeadPlayers.TryGetValue(mcsLeadPlayerId, out var mcsAILeadPlayer))
            {
                return mcsAILeadPlayer;
            }
            return null;
        }

        public List<McsAILeadPlayer> GetAllMcsAILeadPlayer()
        {
            var mcsAILeadPlayers = _mcsAILeadPlayers.Values.ToList();
            return mcsAILeadPlayers;
        }

        /// <summary>
        /// 即便是死亡的McsBotPlayer也会获取
        /// </summary>
        public List<Player> GetAllMcsBotPlayer()
        {
            var mcsBotPlayers = new List<Player>();
            foreach (var kvp in _mcsSquadDict.Values)
            {
                mcsBotPlayers.AddRange(kvp.Values);
            }
            return mcsBotPlayers;
        }

        /// <summary>
        /// 只获取活着的McsBotPlayer
        /// </summary>
        public List<Player> GetAllAliveMcsBotPlayer()
        {
            var mcsBotPlayers = new List<Player>();
            foreach (var kvp in _mcsSquadDict.Values)
            {
                foreach (var mcsBotPlayer in kvp.Values)
                {
                    if (mcsBotPlayer.HealthController.IsAlive)
                    {
                        mcsBotPlayers.Add(mcsBotPlayer);
                    }
                }
            }
            return mcsBotPlayers;
        }

        public void UpdateMcsBotPlayerConfig(MongoID mcsLeadPlayerId, McsBotPlayerConfig mcsBotPlayerConfig)
        {
            if (!mcsBotPlayerConfig.EnableLooting)
            {
                var mcsBotPlayers = GetAllMcsSquadMembersByMcsLeadId(mcsLeadPlayerId);
                foreach (var mcsBotPlayer in mcsBotPlayers)
                {
                    var botOwner = mcsBotPlayer?.AIData?.BotOwner;
                    if (botOwner == null)
                    {
                        continue;
                    }
                    var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                    if (mcsBotPlayerData != null)
                    {
                        mcsBotPlayerData.IsLooting = false;
                    }
                }
            }
            McsLeadPlayerConfigs.AddOrUpdate(
                mcsLeadPlayerId,
                id => mcsBotPlayerConfig,
                (id, oldConfig) =>
                {
                    oldConfig.EnableLooting = mcsBotPlayerConfig.EnableLooting;
                    oldConfig.PriceThreshold = mcsBotPlayerConfig.PriceThreshold;
                    oldConfig.KeywordItemText = mcsBotPlayerConfig.KeywordItemText;
                    oldConfig.LootingKeywordItem = mcsBotPlayerConfig.LootingKeywordItem;
                    oldConfig.BlockItemType = mcsBotPlayerConfig.BlockItemType;
                    return oldConfig;
                }
            );
        }

        public void AddPunish(MongoID friendlyFirePlayerId, double diff, bool teamKill, bool punishEveryone)
        {
            var penalty = new FriendlyFirePenalty
            {
                FriendlyFirePlayerId = friendlyFirePlayerId,
                Diff = diff,
                TeamKill = teamKill,
                PunishEveryone = punishEveryone
            };

            _friendlyFireDebouncer.Trigger(friendlyFirePlayerId, penalty);
        }

        private FriendlyFirePenalty MergeFriendlyFirePenalty(FriendlyFirePenalty existing, FriendlyFirePenalty newValue)
        {
            return new FriendlyFirePenalty
            {
                FriendlyFirePlayerId = existing.FriendlyFirePlayerId,
                Diff = existing.Diff + newValue.Diff,
                TeamKill = newValue.TeamKill,
                PunishEveryone = newValue.PunishEveryone
            };
        }

        public void SendCompensation(MongoID mcsLeadPlayerId)
        {
            TasksExtensions.HandleExceptions(McsRequestHandler.SendCompensationRequest(new Compensation
            {
                McsLeadPlayerId = mcsLeadPlayerId
            }));
        }

        public HashSet<MongoID> GetAllMcsBotPlayerIdInRaid()
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !IsHost)
            {
                foreach (var mcsBotPlayerId in _allMcsBotPlayerIdInRaid)
                {
                    var mcsBotPlayer = TryGetMcsBotPlayer(mcsBotPlayerId);
                    if (mcsBotPlayer == null)
                    {
                        continue;
                    }
                    mcsBotPlayer.Profile.Info.GroupId = "Fika";
                    mcsBotPlayer.Profile.Info.TeamId = "Fika";
                }
            }
            return _allMcsBotPlayerIdInRaid;
        }

        private void SendPunishRequests(Dictionary<MongoID, FriendlyFirePenalty> penalties)
        {
            if (!Gameloop.IsVaildGameWorld || !IsHost)
            {
                return;
            }

            foreach (var kvp in penalties)
            {
                if (kvp.Value != null)
                {
                    TasksExtensions.HandleExceptions(McsRequestHandler.SendPunishRequest(kvp.Value));
                }
            }
        }

        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            _friendlyFireDebouncer = new Debouncer<MongoID, FriendlyFirePenalty>(
                this,
                10f,
                SendPunishRequests,
                MergeFriendlyFirePenalty
            );
            TasksExtensions.HandleExceptions(RequestAllMcsBotPlayerIdInRaid());
            TasksExtensions.HandleExceptions(RequestMySquadMcsBotPlayerIds());
        }

        public bool IsMcsMemberPlayer(MongoID mcsBotPlayerId, out Player mcsLeadPlayer)
        {
            mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mcsLeadPlayer == null)
            {
                return false;
            }
            var squadMembers = _mcsSquadDict.GetOrAdd(mcsLeadPlayer.ProfileId, _ => new());
            return squadMembers.Keys.Contains(mcsBotPlayerId);
        }

        private async Task RequestMySquadMcsBotPlayerIds()
        {
            var mySquadMcsBotPlayerIds = await McsRequestHandler.RequestMySquadMcsBotPlayerIds(new()
            {
                Side = MatchmakerAcceptScreenShowPatch.CurrentType
            });

            if (mySquadMcsBotPlayerIds.Count == 0)
            {
                return;
            }

            var myPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (myPlayer == null)
            {
                return;
            }

            foreach (var mcsBotPlayerId in mySquadMcsBotPlayerIds)
            {
                AddMcsSquadMember(myPlayer.ProfileId, mcsBotPlayerId);
            }
        }

        private async Task RequestAllMcsBotPlayerIdInRaid()
        {
            _allMcsBotPlayerIdInRaid = await McsRequestHandler.GetAllMcsBotPlayerIdInRaid(new()
            {
                Side = MatchmakerAcceptScreenShowPatch.CurrentType
            });
        }

        public override void OnRaidEnded()
        {
            base.OnRaidEnded();
            if (_friendlyFireDebouncer != null)
            {
                _friendlyFireDebouncer.Flush();
                _friendlyFireDebouncer.Clear();
            }
            _friendlyFireDebouncer = null;
            if (_mcsDeadBotPlayerIds != null)
            {
                _mcsDeadBotPlayerIds.Clear();
            }
            if (_mcsSquadDict != null)
            {
                _mcsSquadDict.Clear();
            }
            if (_mcsLeadPlayerIds != null)
            {
                _mcsLeadPlayerIds.Clear();
            }
            if (_mcsAILeadPlayers != null)
            {
                _mcsAILeadPlayers.Clear();
            }
            if (_allMcsBotPlayerIdInRaid != null)
            {
                _allMcsBotPlayerIdInRaid.Clear();
            }
            if (McsLeadPlayerConfigs != null)
            {
                McsLeadPlayerConfigs.Clear();
            }
            if (McsTransitBotPlayers != null)
            {
                foreach (var transitMembers in McsTransitBotPlayers.Values)
                {
                    transitMembers.Clear();
                }
            }
            IsHost = false;
        }

        private void UpdateMcsBotPlayerConfig(ConfigEntrySettingChangedEvent @event)
        {
            if (!IsHost)
            {
                return;
            }

            UpdateMcsBotPlayerConfig(@event.McsBotPlayerConfig.McsLeadPlayerId, @event.McsBotPlayerConfig);
        }

        public List<MongoID> GetMySquadMcsBotPlayerIds(out Player mcsLeadPlayer)
        {
            mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mcsLeadPlayer == null)
            {
                return new();
            }
            var squadMembers = _mcsSquadDict.GetOrAdd(mcsLeadPlayer.ProfileId, _ => new());
            return squadMembers.Keys.ToList();
        }

        public Player TryGetMcsBotPlayer(MongoID mcsBotPlayerId, MongoID? mcsLeadPlayerId = null)
        {
            if (mcsLeadPlayerId.HasValue)
            {
                var squadMembers = _mcsSquadDict.GetOrAdd(mcsLeadPlayerId.Value, _ => new());
                foreach (var mcsBotPlayer in squadMembers.Values)
                {
                    if (mcsBotPlayer.ProfileId == mcsBotPlayerId)
                    {
                        return mcsBotPlayer;
                    }
                }
                return null;
            }

            foreach (var kvp in _mcsSquadDict.Values)
            {
                foreach ((var _mcsBotPlayerId, var _mcsBotPlayer) in kvp)
                {
                    if (_mcsBotPlayerId == mcsBotPlayerId)
                    {
                        return _mcsBotPlayer;
                    }
                }
            }
            return null;
        }
    }
}