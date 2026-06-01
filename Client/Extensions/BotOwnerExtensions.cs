
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT;
using MiyakoCarryService.Client.Bots.BotBehaviors;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class BotOwnerExtensions
    {
        private static PlayerDataMgr PlayerDataMgr => MgrAccessor.Get<PlayerDataMgr>();

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        private static SubtitlesMgr SubtitlesMgr => MgrAccessor.Get<SubtitlesMgr>();

        private static readonly ConditionalWeakTable<BotOwner, McsBotPlayerData> _datas = new();
        
        extension(BotOwner botOwner)
        {
            public bool IsMcsBotPlayer => McsMgr.IsMcsBotPlayer(botOwner.ProfileId);

            public McsBotPlayerData GetMcsBotPlayerData()
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

            public List<BotBehavior> GetBotBehaviors()
            {
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData == null)
                {
                    return new();
                }
                return mcsBotPlayerData.BotBehaviors;
            }

            public void SetMcsBotPlayerData(McsBotPlayerData mcsBotPlayerData)
            {
                _datas.AddOrUpdate(botOwner, mcsBotPlayerData);
            }

            public void TalkMsg(McsMsg msg)
            {
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData == null)
                {
                    return;
                }
                SubtitlesMgr.TalkMsg(botOwner.GetMcsBotPlayerData().LeadPlayer, botOwner.GetPlayer, msg);
            }

            public void TalkMsg(Player mcsLeadPlayer, Player mcsBotPlayer, McsMsg msg)
            {
                SubtitlesMgr.TalkMsg(mcsLeadPlayer, mcsBotPlayer, msg);
            }
        }
    }
}