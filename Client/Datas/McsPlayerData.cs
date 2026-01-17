

using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Bots.BotBehaviors;

namespace MiyakoCarryService.Client.Datas
{
    internal class McsPlayerData : BaseData
    {
        public BotOwner Self { get; private set; }
        public Player MyBossPlayer { get; private set; }
        public List<BotBehavior> BotBehaviors { get; private set; }
        public McsPlayerData(BotOwner self, Player myBossPlayer)
        {
            Self = self;
            MyBossPlayer = myBossPlayer;
            BotBehaviors = [new BotCarryServiceChecker(Self, MyBossPlayer)];
        }
    }
}