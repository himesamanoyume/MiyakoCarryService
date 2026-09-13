using EFT.Vaulting;

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
            if (distance <= minDistantToInteract || distance > minDistantToInteract + 0.01f)
            {
                return;
            }

            distance = minDistantToInteract;
        }
    }
}
