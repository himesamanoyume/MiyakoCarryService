using EFT;
using HarmonyLib;

namespace MiyakoCarryService.Client.Utils
{
    public static class MovementContextUtils
    {
        private static readonly AccessTools.FieldRef<MovementContext, Player> _playerRef = AccessTools.FieldRefAccess<MovementContext, Player>("_player");

        public static Player GetPlayer(MovementContext movementContext)
        {
            if (movementContext == null)
            {
                return null;
            }

            return _playerRef(movementContext);
        }
    }
}
