
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Mgrs;

namespace MiyakoCarryService.Client.Extensions
{
    internal static class BotOwnerExtensions
    {
        private static PlayerDataMgr PlayerDataMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<PlayerDataMgr>();
            }
        }

        private static SquadMgr SquadMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }
        
        extension(BotOwner botOwner)
        {
            public bool IsMcsPlayer => SquadMgr.IsMcsPlayer(botOwner.ProfileId);

            public McsPlayerData GetMcsData()
            {
                var mcsPlayerDatas = PlayerDataMgr.GetMcsPlayerDatas();
                foreach (var mcsPlayerData in mcsPlayerDatas)
                {
                    if (mcsPlayerData.BotOwner == botOwner)
                    {
                        return mcsPlayerData;
                    }
                }
                return null;
            }
        }
    }
}