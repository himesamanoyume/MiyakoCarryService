
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT;
using MiyakoCarryService.Client.Bots.BotBehaviors;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Mgrs;

namespace MiyakoCarryService.Client.Extensions
{
    internal static class BotOwnerExtensions
    {
        private static readonly ConditionalWeakTable<BotOwner, McsPlayerData> _botDatas = new();
        private static SquadMgr SquadMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }
        
        extension(BotOwner botOwner)
        {
            public McsPlayerData GetMcsData()
            {
                return _botDatas.GetValue(botOwner, InitMcsData);
            }

            private McsPlayerData InitMcsData()
            {
                var mcsBossPlayer = SquadMgr.GetMcsBossPlayer(botOwner.ProfileId);
                return new McsPlayerData(botOwner, mcsBossPlayer);
            }

            public List<BotBehavior> GetBotBehaviors()
            {
                return botOwner.GetMcsData().BotBehaviors;
            }

            public bool IsMcsPlayer => botOwner.GetMcsData().MyBossPlayer != null;

            public Player McsBossPlayer => botOwner.GetMcsData().MyBossPlayer;
        }
    }
}