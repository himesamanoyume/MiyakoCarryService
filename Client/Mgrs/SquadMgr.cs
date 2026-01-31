

using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Misc;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class SquadMgr : BaseMgr<SquadMgr>
    {
        private Dictionary<MongoID, Dictionary<MongoID, BotOwner>> _mcsSquadDict = new();
        private HashSet<MongoID> _mcsBossPlayerIds = new();
        private HashSet<MongoID> _mcsBotPlayerIds = new();
        private Dictionary<MongoID, McsAIBossPlayer> _mcsAIBossPlayers = new();

        public sealed override void Start()
        {
            base.Start();
        }

        public void AddMcsSquadMember(MongoID mcsBossPlayerId, MongoID mcsBotPlayerId, BotOwner botOwner, McsAIBossPlayer mcsAIBossPlayer)
        {
            if (!_mcsSquadDict.TryGetValue(mcsBossPlayerId, out var squadMembers))
            {
                squadMembers = new(){{mcsBotPlayerId, botOwner}};
                _mcsSquadDict.Add(mcsBossPlayerId, squadMembers);
            }
            else
            {
                squadMembers.Add(mcsBotPlayerId, botOwner);
            }
            _mcsBossPlayerIds.Add(mcsBossPlayerId);
            _mcsBotPlayerIds.Add(mcsBotPlayerId);
            _mcsAIBossPlayers[mcsBossPlayerId] = mcsAIBossPlayer;
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
            _mcsSquadDict.Clear();
            _mcsBossPlayerIds.Clear();
            _mcsBotPlayerIds.Clear();
            _mcsAIBossPlayers.Clear();
        }
    }
}