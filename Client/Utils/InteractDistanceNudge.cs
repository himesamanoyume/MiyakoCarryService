using EFT.Vaulting;
using MiyakoCarryService.Client.Bots.Navigation;

namespace MiyakoCarryService.Client.Utils
{
    internal static class InteractDistanceNudge
    {
        public static void Nudge(IVaultingRestrictions restrictions, ref float distance)
        {
            if (restrictions == null)
            {
                return;
            }

            var minDistantToInteract = restrictions.MinDistantToInteract;
            var tolerance = NavGapExecutor.McsVaultScope ? 0.3f : 0.01f;
            if (distance <= minDistantToInteract || distance > minDistantToInteract + tolerance)
            {
                return;
            }

            distance = minDistantToInteract;
        }
    }
}
