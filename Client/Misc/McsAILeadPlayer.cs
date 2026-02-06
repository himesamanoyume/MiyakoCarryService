
using EFT;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Client.Misc
{
    internal class McsAILeadPlayer : AIBossPlayer
    {
        public McsBotPlayerConfig McsBotPlayerConfig;
        public BotOwner DeputyLeader;
        public McsAILeadPlayer(Player player, McsBotPlayerConfig mcsBotPlayerConfig) : base(player)
        {
            McsBotPlayerConfig = mcsBotPlayerConfig;
        }

        public void SetDeputyLeader(BotOwner deputyLeader)
        {
            DeputyLeader = deputyLeader;
        }
    }
}