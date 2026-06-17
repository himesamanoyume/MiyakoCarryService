
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Patches.Events;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class McsMgr : BaseMgr<McsMgr>
    {
        private Dictionary<MongoID, Dictionary<MongoID, BotOwner>> _mcsSquadDict = new();
        private HashSet<MongoID> _mcsLeadPlayerIds = new();
        private HashSet<MongoID> _allMcsBotPlayerIdInRaid = new();
        private HashSet<MongoID> _mcsDeadBotPlayerIds = new();
        private Dictionary<MongoID, McsAILeadPlayer> _mcsAILeadPlayers = new();
        public Dictionary<MongoID, Dictionary<MongoID, GroupPlayerViewModelClass>> McsTransitBotPlayers = new();
        private Debouncer<MongoID, FriendlyFirePenalty> _friendlyFireDebouncer;
        public ConcurrentDictionary<MongoID, McsBotPlayerConfig> McsLeadPlayerConfigs = new();

        public bool IsHost = false;

        public sealed override void Start()
        {
            base.Start();
            EventMgr.Subscribe<ConfigEntrySettingChangedEvent>(UpdateMcsBotPlayerConfig, this);
            EventMgr.Subscribe<McsLeadPlayerExtractedEvent>(HandleMcsLeadPlayerExtracted, this);
        }

        public void AddMcsSquadMember(MongoID mcsLeadPlayerId, MongoID mcsBotPlayerId, BotOwner botOwner, McsAILeadPlayer mcsAILeadPlayer)
        {
            if (!_mcsSquadDict.TryGetValue(mcsLeadPlayerId, out var squadMembers))
            {
                squadMembers = new() { { mcsBotPlayerId, botOwner } };
                _mcsSquadDict.Add(mcsLeadPlayerId, squadMembers);
            }
            else
            {
                if (!squadMembers.ContainsKey(mcsBotPlayerId))
                {
                    squadMembers.Add(mcsBotPlayerId, botOwner);
                }
            }
            _mcsLeadPlayerIds.Add(mcsLeadPlayerId);
            _allMcsBotPlayerIdInRaid.Add(mcsBotPlayerId);
            _mcsAILeadPlayers[mcsLeadPlayerId] = mcsAILeadPlayer;
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

        public IEnumerable<BotOwner> GetAllMcsSquadMembersByMcsLeadId(MongoID mcsLeadPlayerId)
        {
            _mcsLeadPlayerIds.Add(mcsLeadPlayerId);
            if (_mcsSquadDict.TryGetValue(mcsLeadPlayerId, out var squadMembers))
            {
                return squadMembers.Values;
            }
            return null;
        }

        public IEnumerable<BotOwner> GetAllAliveMcsSquadMembersByMcsLeadId(MongoID mcsLeadPlayerId)
        {
            _mcsLeadPlayerIds.Add(mcsLeadPlayerId);
            if (_mcsSquadDict.TryGetValue(mcsLeadPlayerId, out var squadMembers))
            {
                foreach (var botOwner in squadMembers.Values)
                {
                    if (botOwner.HealthController.IsAlive)
                    {
                        yield return botOwner;
                    }
                }
            }
            yield return null;
        }

        public bool IsMcsBotPlayer(MongoID mcsBotPlayerId)
        {
            return _allMcsBotPlayerIdInRaid.Contains(mcsBotPlayerId);
        }

        public bool IsMyMcsBotPlayer(MongoID mcsLeadPlayerId, MongoID mcsBotPlayerId)
        {
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
        /// 即便是死亡的BotOwner也会获取
        /// </summary>
        public List<BotOwner> GetAllMcsBotPlayer()
        {
            var mcsBotPlayers = new List<BotOwner>();
            foreach (var botOwners in _mcsSquadDict.Values)
            {
                mcsBotPlayers.AddRange(botOwners.Values);
            }
            return mcsBotPlayers;
        }

        /// <summary>
        /// 只获取活着的BotOwner
        /// </summary>
        public List<BotOwner> GetAllAliveMcsBotPlayer()
        {
            var mcsBotPlayers = new List<BotOwner>();
            foreach (var botOwners in _mcsSquadDict.Values)
            {
                foreach (var botOwner in botOwners.Values)
                {
                    if (!botOwner.IsDead)
                    {
                        mcsBotPlayers.Add(botOwner);
                    }
                }
            }
            return mcsBotPlayers;
        }

        public void UpdateMcsBotPlayerConfig(MongoID mcsLeadPlayerId, McsBotPlayerConfig mcsBotPlayerConfig)
        {
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
                    var mcsBotPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(mcsBotPlayerId);
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
            if (!_gameloop.IsVaildGameWorld || !IsHost)
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

        protected override void OnRaidStarted()
        {
            base.OnRaidStarted();
            _friendlyFireDebouncer = new Debouncer<MongoID, FriendlyFirePenalty>(
                this,
                10f,
                SendPunishRequests,
                MergeFriendlyFirePenalty
            );
            TasksExtensions.HandleExceptions(RequestAllMcsBotPlayerIdInRaid());
        }

        private async Task RequestAllMcsBotPlayerIdInRaid()
        {
            _allMcsBotPlayerIdInRaid = await McsRequestHandler.GetAllMcsBotPlayerIdInRaid(new()
            {
                Side = MatchmakerAcceptScreenShowPatch.CurrentType
            });
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
            if (_friendlyFireDebouncer != null)
            {
                _friendlyFireDebouncer.Flush();
                _friendlyFireDebouncer.Clear();
            }
            _friendlyFireDebouncer = null;
            _mcsDeadBotPlayerIds.Clear();
            _mcsSquadDict.Clear();
            _mcsLeadPlayerIds.Clear();
            _mcsAILeadPlayers.Clear();
            _allMcsBotPlayerIdInRaid.Clear();
            McsLeadPlayerConfigs.Clear();
            foreach (var transitMembers in McsTransitBotPlayers.Values)
            {
                transitMembers.Clear();
            }
            IsHost = false;
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
            OnRaidEnded();
        }

        private void UpdateMcsBotPlayerConfig(ConfigEntrySettingChangedEvent @event)
        {
            if (!IsHost)
            {
                return;
            }

            UpdateMcsBotPlayerConfig(@event.McsBotPlayerConfig.McsLeadPlayerId, @event.McsBotPlayerConfig);
        }

        private void HandleMcsLeadPlayerExtracted(McsLeadPlayerExtractedEvent @event)
        {
            if (!IsHost)
            {
                return;
            }

            var botOwners = GetAllAliveMcsSquadMembersByMcsLeadId(@event.McsLeadPlayerId);
            foreach (var botOwner in botOwners)
            {
                if (botOwner == null)
                {
                    continue;
                }
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData == null)
                {
                    continue;
                }
                mcsBotPlayerData.SetDecision(null, EDecision.ShouldExfil);
            }
        }
    }
}