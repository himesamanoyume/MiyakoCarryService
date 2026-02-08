

using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Misc;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class SquadMgr : BaseMgr<SquadMgr>
    {
        private Dictionary<MongoID, Dictionary<MongoID, BotOwner>> _mcsSquadDict = new();
        private HashSet<MongoID> _mcsLeadPlayerIds = new();
        private HashSet<MongoID> _mcsBotPlayerIds = new();
        private HashSet<MongoID> _mcsDeadBotPlayerIds = new();
        private Dictionary<MongoID, McsAILeadPlayer> _mcsAILeadPlayers = new();
        public Dictionary<MongoID, Dictionary<MongoID, GroupPlayerViewModelClass>> McsTransitBotPlayers = new();

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

        protected override void OnRaidStarted()
        {
            base.OnRaidStarted();
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
            _mcsDeadBotPlayerIds.Clear();
            _mcsSquadDict.Clear();
            _mcsLeadPlayerIds.Clear();
            _mcsBotPlayerIds.Clear();
            _mcsAILeadPlayers.Clear();
            foreach (var transitMembers in McsTransitBotPlayers.Values)
            {
                transitMembers.Clear();
            }
        }
    }
}