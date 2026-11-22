using EFT;

namespace MiyakoCarryService.Client.Utils
{
    public static class MovementContextUtils
    {
        public static Player GetPlayer(MovementContext movementContext)
        {
            if (movementContext == null)
            {
                return null;
            }

            return movementContext._player;
        }
    }
}
