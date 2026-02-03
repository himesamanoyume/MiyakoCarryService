

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
        private HashSet<MongoID> _mcsBossPlayerIds = new();
        private HashSet<MongoID> _mcsBotPlayerIds = new();
        private HashSet<MongoID> _mcsDeadBotPlayerIds = new();
        private Dictionary<MongoID, McsAIBossPlayer> _mcsAIBossPlayers = new();
        public Dictionary<MongoID, Dictionary<MongoID, GroupPlayerViewModelClass>> McsTransitBotPlayers = new();

        public sealed override void Start()
        {
            base.Start();
        }

        public void AddMcsSquadMember(MongoID mcsBossPlayerId, MongoID mcsBotPlayerId, BotOwner botOwner, McsAIBossPlayer mcsAIBossPlayer)
        {
            if (!_mcsSquadDict.TryGetValue(mcsBossPlayerId, out var squadMembers))
            {
                squadMembers = new() { { mcsBotPlayerId, botOwner } };
                _mcsSquadDict.Add(mcsBossPlayerId, squadMembers);
            }
            else
            {
                if (!squadMembers.ContainsKey(mcsBotPlayerId))
                {
                    squadMembers.Add(mcsBotPlayerId, botOwner);
                }
            }
            _mcsBossPlayerIds.Add(mcsBossPlayerId);
            _mcsBotPlayerIds.Add(mcsBotPlayerId);
            _mcsAIBossPlayers[mcsBossPlayerId] = mcsAIBossPlayer;
        }

        public void AddMcsSquadMemberToTransit(MongoID mcsBossPlayerId, BotOwner botOwner)
        {
            if (!McsTransitBotPlayers.TryGetValue(mcsBossPlayerId, out var transitMembers))
            {
                transitMembers = new();
                McsTransitBotPlayers.Add(mcsBossPlayerId, transitMembers);
            }

            if (!transitMembers.TryGetValue(botOwner.ProfileId, out var groupPlayerViewModelClasses))
            {
                var groupPlayerViewModelClass = new GroupPlayerViewModelClass(new GroupPlayerDataClass
                {
                    AccountId = botOwner.AccountId,
                    Id = botOwner.ProfileId,
                    Info = new()
                    {
                        Level = botOwner.Profile.Info.Level,
                        PrestigeLevel = botOwner.Profile.Info.PrestigeLevel,
                        MemberCategory = botOwner.Profile.Info.MemberCategory,
                        SelectedMemberCategory = botOwner.Profile.Info.SelectedMemberCategory,
                        Nickname = botOwner.Profile.Info.Nickname,
                        Side = botOwner.Profile.Info.Side,
                        SavageLockTime = botOwner.Profile.Info.SavageLockTime,
                        SavageNickname = botOwner.Profile.Info.Nickname,
                        GameVersion = botOwner.Profile.Info.GameVersion,
                        HasCoopExtension = botOwner.Profile.Info.HasCoopExtension,
                        Health = botOwner.Profile.Health
                    },
                    PlayerVisualRepresentation = new(new()
                    {
                        Level = botOwner.Profile.Info.Level,
                        MemberCategory = botOwner.Profile.Info.MemberCategory,
                        SelectedMemberCategory = botOwner.Profile.Info.SelectedMemberCategory,
                        Nickname = botOwner.Profile.Info.Nickname,
                        Side = botOwner.Profile.Info.Side,
                        Health = botOwner.Profile.Health
                    }, botOwner.Profile.Customization, botOwner.Profile.Inventory.Equipment)
                });
                transitMembers.Add(botOwner.ProfileId, groupPlayerViewModelClass);
            }
        }

        public IEnumerable<BotOwner> GetAllMcsSquadMembersByMcsBossId(MongoID mcsBossPlayerId)
        {
            _mcsBossPlayerIds.Add(mcsBossPlayerId);
            if (_mcsSquadDict.TryGetValue(mcsBossPlayerId, out var squadMembers))
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

        public bool IsMcsBossPlayer(MongoID mcsBossPlayerId)
        {
            return _mcsBossPlayerIds.Contains(mcsBossPlayerId);
        }

        public Player GetMcsBossPlayerByMcsBotPlayerId(MongoID mcsBotPlayerId)
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

        public McsAIBossPlayer GetMcsAIBossPlayerByMcsBossPlayerId(MongoID mcsBossPlayerId)
        {
            if (_mcsAIBossPlayers.TryGetValue(mcsBossPlayerId, out var mcsAIBossPlayer))
            {
                return mcsAIBossPlayer;
            }
            MiyakoCarryServicePlugin.Logger.LogError("mcsAIBossPlayer 返回空");
            return null;
        }

        public List<McsAIBossPlayer> GetAllMcsAIBossPlayer()
        {
            var mcsAIBossPlayers = _mcsAIBossPlayers.Values.ToList();
            return mcsAIBossPlayers;
        }

        public List<BotOwner> GetAllMcsBotPlayer()
        {
            var mcsBotPlayers = new List<BotOwner>();
            foreach (var botOwners in _mcsSquadDict.Values)
            {
                mcsBotPlayers.AddRange(botOwners.Values);
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
            _mcsBossPlayerIds.Clear();
            _mcsBotPlayerIds.Clear();
            _mcsAIBossPlayers.Clear();
            foreach (var transitMembers in McsTransitBotPlayers.Values)
            {
                transitMembers.Clear();
            }
        }
    }
}