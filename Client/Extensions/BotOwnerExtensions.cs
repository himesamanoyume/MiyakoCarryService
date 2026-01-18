
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
        
        extension(BotOwner botOwner)
        {
            public McsPlayerData GetMcsData()
            {
                var allMcsPlayerData = PlayerDataMgr.GetMcsPlayerAllDatas();
                foreach (var mcsPlayerData in allMcsPlayerData)
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