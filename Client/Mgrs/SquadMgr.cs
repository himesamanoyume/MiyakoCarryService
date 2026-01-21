

using System.Collections.Concurrent;
using System.Collections.Generic;
using Comfort.Common;
using EFT;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class SquadMgr : BaseMgr<SquadMgr>
    {
        private ConcurrentDictionary<MongoID, ConcurrentDictionary<MongoID, BotOwner>> _mcsSquadDict = new();
        private HashSet<MongoID> _mcsBossPlayerIds = new();
        private HashSet<MongoID> _mcsBotPlayerIds = new();

        public sealed override void Start()
        {
            base.Start();
        }

        public void AddMcsSquadMember(MongoID mcsBossPlayerId, MongoID mcsBotPlayerId, BotOwner botOwner)
        {
            _mcsSquadDict.GetOrAdd(mcsBossPlayerId, _ => new()).GetOrAdd(mcsBotPlayerId, botOwner);
            _mcsBossPlayerIds.Add(mcsBossPlayerId);
            _mcsBotPlayerIds.Add(mcsBotPlayerId);
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

        protected override void Reset()
        {
            _mcsSquadDict.Clear();
            _mcsBossPlayerIds.Clear();
            _mcsBotPlayerIds.Clear();
        }
    }
}