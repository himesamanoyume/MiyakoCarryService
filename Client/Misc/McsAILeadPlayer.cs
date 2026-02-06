
using EFT;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Client.Misc
{
    internal class McsAILeadPlayer : AIBossPlayer
    {
        public McsBotPlayerConfig McsBotPlayerConfig;
        public McsAILeadPlayer(Player player, McsBotPlayerConfig mcsBotPlayerConfig) : base(player)
        {
            McsBotPlayerConfig = mcsBotPlayerConfig;
        }
    }
}