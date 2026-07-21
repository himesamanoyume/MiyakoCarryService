
using EFT;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class ProfileExtensions
    {
        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        
        extension(Profile profile)
        {
            public string McsNickname => MiyakoCarryServicePlugin.ShowBrevityCode.Value ? "Rabbit" + McsMgr.GetMcsBotPlayerIndex(profile.Id, false) : profile.Nickname;
        }
    }
}