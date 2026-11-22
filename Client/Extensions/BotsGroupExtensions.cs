using EFT;

namespace MiyakoCarryService.Client.Extensions
{
    public static class BotsGroupExtensions
    {
        public static void AddEnemy(this BotsGroup group, IPlayer player)
        {
            if (group == null || player == null)
            {
                return;
            }

            group.BotGroupHostility.AddEnemyAction(player, new BotGroupEnemyInfo(player, group));
        }
    }
}
