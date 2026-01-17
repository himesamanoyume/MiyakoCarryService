

using System.Collections.Concurrent;
using System.Collections.Generic;
using Comfort.Common;
using EFT;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class SquadMgr : BaseMgr<SquadMgr>
    {
        private ConcurrentDictionary<MongoID, ConcurrentDictionary<MongoID, BotOwner>> _mcsSquadDict = new();

        public sealed override void Start()
        {
            base.Start();
        }

        public void AddMcsSquadMember(MongoID bossSessionId, MongoID csPlayerSessionId, BotOwner botOwner)
        {
            _mcsSquadDict.GetOrAdd(bossSessionId, _ => new()).GetOrAdd(csPlayerSessionId, botOwner);
        }

        public IEnumerable<BotOwner> GetAllMcsSquadMembersByBossId(MongoID bossSessionId)
        {
            return _mcsSquadDict.GetOrAdd(bossSessionId, _ => new()).Values;
        }

        public bool IsMcsPlayer(MongoID csPlayerSessionId)
        {
            foreach (var mcsSquad in _mcsSquadDict.Values)
            {
                if (mcsSquad.ContainsKey(csPlayerSessionId))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsMcsBossPlayer(MongoID bossSessionId)
        {
            return _mcsSquadDict.ContainsKey(bossSessionId);
        }

        public Player GetMcsBossPlayer(MongoID csPlayerSessionId)
        {
            foreach (var kvp in _mcsSquadDict)
            {
                if (kvp.Value.ContainsKey(csPlayerSessionId))
                {
                    return Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(kvp.Key);
                }
            }
            return null;
        }

        protected override void Reset()
        {
            _mcsSquadDict.Clear();
        }
    }
}