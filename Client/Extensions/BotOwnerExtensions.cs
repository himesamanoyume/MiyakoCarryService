
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

        private static SubTitleMgr SubTitleMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SubTitleMgr>();
            }
        }
        
        extension(BotOwner botOwner)
        {
            public bool IsMcsBotPlayer => SquadMgr.IsMcsBotPlayer(botOwner.ProfileId);

            public McsBotPlayerData GetMcsBotData()
            {
                var mcsBotPlayerDatas = PlayerDataMgr.GetMcsBotPlayerDatas();
                foreach (var mcsBotPlayerData in mcsBotPlayerDatas)
                {
                    if (mcsBotPlayerData.BotOwner == botOwner)
                    {
                        return mcsBotPlayerData;
                    }
                }
                return null;
            }

            public void ShowSubtitleMsg(string msg)
            {
                SubTitleMgr.ShowMcsBotPlayerMsg(botOwner.ProfileId, msg);
            }
        }
    }
}