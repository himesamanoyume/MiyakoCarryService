
using System.Runtime.CompilerServices;
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

        private static readonly ConditionalWeakTable<BotOwner, McsBotPlayerData> _datas = new();
        
        extension(BotOwner botOwner)
        {
            public bool IsMcsBotPlayer => SquadMgr.IsMcsBotPlayer(botOwner.ProfileId);

            public McsBotPlayerData GetMcsBotData()
            {
                if (_datas.TryGetValue(botOwner, out var mcsBotPlayerData))
                {
                    return mcsBotPlayerData;
                }

                var mcsBotPlayerDatas = PlayerDataMgr.GetMcsBotPlayerDatas();
                foreach (var _mcsBotPlayerData in mcsBotPlayerDatas)
                {
                    if (_mcsBotPlayerData.BotOwner == botOwner)
                    {
                        _datas.Add(botOwner, _mcsBotPlayerData);
                        return _mcsBotPlayerData;
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