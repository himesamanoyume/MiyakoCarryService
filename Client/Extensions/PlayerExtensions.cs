
using System.Runtime.CompilerServices;
using EFT;

namespace MiyakoCarryService.Client.Extensions
{
    public static class PlayerExtensions
    {
        private static readonly ConditionalWeakTable<Player, GamePlayerOwner> _gamePlayerOwners = new();

        extension(Player player)
        {
            public GamePlayerOwner GetGamePlayerOwner()
            {
                if (_gamePlayerOwners.TryGetValue(player, out var gamePlayerOwner))
                {
                    return gamePlayerOwner;
                }
                else
                {
                    var _gamePlayerOwner = player.GetComponentInChildren<GamePlayerOwner>();
                    if (_gamePlayerOwner != null)
                    {
                        _gamePlayerOwners.Add(player, _gamePlayerOwner);
                        return _gamePlayerOwner;
                    }
                    return null;
                }
            }

            public BotOwner BotOwner => player?.AIData?.BotOwner;
        }
    }
}
