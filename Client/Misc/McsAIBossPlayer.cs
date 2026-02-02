
using EFT;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Client.Misc
{
    internal class McsAIBossPlayer : AIBossPlayer
    {
        public McsBotPlayerConfig McsBotPlayerConfig;
        public McsAIBossPlayer(Player player, McsBotPlayerConfig mcsBotPlayerConfig) : base(player)
        {
            McsBotPlayerConfig = mcsBotPlayerConfig;
        }
    }
}