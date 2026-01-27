

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Misc;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class SquadMgr : BaseMgr<SquadMgr>
    {
        private ConcurrentDictionary<MongoID, ConcurrentDictionary<MongoID, BotOwner>> _mcsSquadDict = new();
        private HashSet<MongoID> _mcsBossPlayerIds = new();
        private HashSet<MongoID> _mcsBotPlayerIds = new();
        private ConcurrentDictionary<MongoID, McsAIBossPlayer> _mcsAIBossPlayers = new();

        public sealed override void Start()
        {
            base.Start();
        }

        public void AddMcsSquadMember(MongoID mcsBossPlayerId, MongoID mcsBotPlayerId, BotOwner botOwner, McsAIBossPlayer mcsAIBossPlayer)
        {
            _mcsSquadDict.GetOrAdd(mcsBossPlayerId, _ => new()).GetOrAdd(mcsBotPlayerId, botOwner);
            _mcsBossPlayerIds.Add(mcsBossPlayerId);
            _mcsBotPlayerIds.Add(mcsBotPlayerId);
            _mcsAIBossPlayers.GetOrAdd(mcsBossPlayerId, _ => mcsAIBossPlayer);
        }

        public IEnumerable<BotOwner> GetAllMcsSquadMembersByMcsBossId(MongoID mcsBossPlayerId)
        {
            _mcsBossPlayerIds.Add(mcsBossPlayerId);
            return _mcsSquadDict.GetOrAdd(mcsBossPlayerId, _ => new()).Values;
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

        public McsAIBossPlayer GetMcsAIBossPlayerByMcsBossId(MongoID mcsBossPlayerId)
        {
            if (_mcsAIBossPlayers.TryGetValue(mcsBossPlayerId, out var mcsAIBossPlayer))
            {
                return mcsAIBossPlayer;
            }
            return null;
        }

        public List<McsAIBossPlayer> GetAllMcsAIBossPlayer()
        {
            return _mcsAIBossPlayers.Values.ToList();
        }

        protected override void Reset()
        {
            _mcsSquadDict.Clear();
            _mcsBossPlayerIds.Clear();
            _mcsBotPlayerIds.Clear();
            _mcsAIBossPlayers.Clear();
        }

        protected override void OnGameStarted()
        {
            throw new System.NotImplementedException();
        }
    }
}