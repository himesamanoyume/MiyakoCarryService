

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
        private HashSet<MongoID> _mcsPlayerIds = new();

        public sealed override void Start()
        {
            base.Start();
        }

        public void AddMcsSquadMember(MongoID bossSessionId, MongoID csPlayerSessionId, BotOwner botOwner)
        {
            _mcsSquadDict.GetOrAdd(bossSessionId, _ => new()).GetOrAdd(csPlayerSessionId, botOwner);
            _mcsBossPlayerIds.Add(bossSessionId);
            _mcsPlayerIds.Add(csPlayerSessionId);
        }

        public IEnumerable<BotOwner> GetAllMcsSquadMembersByMcsBossId(MongoID bossSessionId)
        {
            _mcsBossPlayerIds.Add(bossSessionId);
            return _mcsSquadDict.GetOrAdd(bossSessionId, _ => new()).Values;
        }

        public bool IsMcsPlayer(MongoID mcsPlayerSessionId)
        {
            return _mcsPlayerIds.Contains(mcsPlayerSessionId);
        }

        public bool IsMcsBossPlayer(MongoID mcsBossSessionId)
        {
            return _mcsBossPlayerIds.Contains(mcsBossSessionId);
        }

        public Player GetMcsBossPlayerByMcsPlayerId(MongoID mcsPlayerSessionId)
        {
            foreach (var mcsSquad in _mcsSquadDict)
            {
                if (mcsSquad.Value.ContainsKey(mcsPlayerSessionId))
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
            _mcsPlayerIds.Clear();
        }
    }
}