using EFT;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class PlayerExtensions
    {

        extension(Player player)
        {
            public GamePlayerOwner GetGamePlayerOwner()
            {
                return InteropDataCache<GamePlayerOwner>.GetOrCreate(player.Pointer, () => player.GetComponentInChildren<GamePlayerOwner>());
            }

            public BotOwner BotOwner => player?.AIData?.BotOwner;
        }

        public static void Clear()
        {
            InteropDataCache<GamePlayerOwner>.Clear();
        }
    }
}