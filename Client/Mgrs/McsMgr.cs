

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class McsMgr : BaseMgr<McsMgr>
    {
        private Dictionary<MongoID, Dictionary<MongoID, BotOwner>> _mcsSquadDict = new();
        private HashSet<MongoID> _mcsLeadPlayerIds = new();

        // _mcsBotPlayerIds 只有Host才会使用
        private HashSet<MongoID> _mcsBotPlayerIds = new();

        // _allMcsBotPlayerIdInRaid 作为队友高亮的必须数据，每个玩家都会进行获取
        private List<MongoID> _allMcsBotPlayerIdInRaid = new();
        private HashSet<MongoID> _mcsDeadBotPlayerIds = new();
        private Dictionary<MongoID, McsAILeadPlayer> _mcsAILeadPlayers = new();
        public Dictionary<MongoID, Dictionary<MongoID, GroupPlayerViewModelClass>> McsTransitBotPlayers = new();
        private ConcurrentDictionary<MongoID, FriendlyFirePenalty> _mcsFriendlyFirePenalties  = new();

        public bool IsHost = false;

        public sealed override void Start()
        {
            base.Start();
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
            _mcsBotPlayerIds.Add(mcsBotPlayerId);
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

        public bool IsMcsBotPlayer(MongoID mcsBotPlayerId)
        {
            return _mcsBotPlayerIds.Contains(mcsBotPlayerId);
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
            MiyakoCarryServicePlugin.Logger.LogError("mcsAILeadPlayer 返回空");
            return null;
        }

        public List<McsAILeadPlayer> GetAllMcsAILeadPlayer()
        {
            var mcsAILeadPlayers = _mcsAILeadPlayers.Values.ToList();
            return mcsAILeadPlayers;
        }

        /// <summary>
        /// 注意：即便是死亡的BotOwner也会获取
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

        public void AddPunish(MongoID friendlyFirePlayerId, double diff, bool teamKill, bool punishEveryone)
        {
            // MiyakoCarryServicePlugin.Logger.LogInfo($"触发惩罚: {diff}");
            FriendlyFirePenalty friendlyFirePenalty;
            if (!_mcsFriendlyFirePenalties.TryGetValue(friendlyFirePlayerId, out friendlyFirePenalty))
            {
                friendlyFirePenalty = _mcsFriendlyFirePenalties.GetOrAdd(friendlyFirePlayerId, _ => new());
            }
            friendlyFirePenalty.FriendlyFirePlayerId = friendlyFirePlayerId;
            friendlyFirePenalty.Diff += diff;
            friendlyFirePenalty.TeamKill = teamKill;
            friendlyFirePenalty.PunishEveryone = punishEveryone;
        }

        public List<MongoID> GetAllMcsBotPlayerIdInRaid()
        {
            return _allMcsBotPlayerIdInRaid;
        }

        private IEnumerator SendPunishRequest(float time)
        {
            var waitTime = new WaitForSeconds(time);
            while (true)
            {
                yield return waitTime;
                if (_gameloop.IsVaildGameWorld)
                {
                    if (!IsHost)
                    {
                        continue;
                    }
                    // MiyakoCarryServicePlugin.Logger.LogInfo("尝试发送惩罚");

                    foreach (var kvp in _mcsFriendlyFirePenalties)
                    {
                        if (kvp.Value != null)
                        {
                            _ = McsRequestHandler.SendPunishRequest(kvp.Value);
                        }
                    }
                    _mcsFriendlyFirePenalties.Clear();
                }
                yield return null;
            }
        }

        protected override void OnRaidStarted()
        {
            base.OnRaidStarted();
            RequestAllMcsBotPlayerIdInRaid();
            StartCoroutine(SendPunishRequest(10f));
        }

        private void RequestAllMcsBotPlayerIdInRaid()
        {
            _allMcsBotPlayerIdInRaid = McsRequestHandler.GetAllMcsBotPlayerIdInRaid();
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
            _mcsDeadBotPlayerIds.Clear();
            _mcsSquadDict.Clear();
            _mcsLeadPlayerIds.Clear();
            _mcsBotPlayerIds.Clear();
            _mcsAILeadPlayers.Clear();
            _allMcsBotPlayerIdInRaid.Clear();
            foreach (var transitMembers in McsTransitBotPlayers.Values)
            {
                transitMembers.Clear();
            }
            IsHost = false;
        }
    }
}