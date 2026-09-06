using EFT;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class PlayerExtensions
    {
        private static readonly AttachedTable<Player, GamePlayerOwner> _gamePlayerOwners = new();

        extension(Player player)
        {
            public GamePlayerOwner GetGamePlayerOwner()
            {
                return _gamePlayerOwners.GetOrCreate(player, key => key.GetComponentInChildren<GamePlayerOwner>());
            }

            public BotOwner BotOwner => player?.AIData?.BotOwner;
        }
    }
}