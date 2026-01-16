

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EFT;
using MiyakoCarryService.Client.Enums;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class BotMgr : BaseMgr<BotMgr>
    {
        private ConcurrentDictionary<MongoID, ConcurrentDictionary<MongoID, BotOwner>> _mcsSquadDict = new();

        public sealed override void Start()
        {
            base.Start();
            _gameloop.Mgrs.Add(EMgrType.BOT, this);
        }

        public void AddMcsSquadMember(MongoID bossSessionId, MongoID csPlayerSessionId, BotOwner botOwner)
        {
            _mcsSquadDict.GetOrAdd(bossSessionId, _ => new()).GetOrAdd(csPlayerSessionId, botOwner);
        }

        public IEnumerable<BotOwner> GetAllMcsSquadMembersByBossId(MongoID bossSessionId)
        {
            return _mcsSquadDict.GetOrAdd(bossSessionId, _ => new()).Values;
        }

        protected override void Reset()
        {
            _mcsSquadDict.Clear();
        }
    }
}