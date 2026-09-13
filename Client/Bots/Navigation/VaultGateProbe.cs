using EFT.Vaulting;

namespace MiyakoCarryService.Client.Bots.Navigation
{
    public static class VaultGateProbe
    {
        public static bool IsAnimatorBusy(VaultingComponent component)
        {
            return component != null
                && (component._vaultingContext.IsAnimatorInTransitionState(0) || component._vaultingContext.PlayerAnimatorIsJumpSetted());
        }

        public static bool IsWithinInteractDistance(float minDistantToInteract, float distance)
        {
            return distance <= minDistantToInteract + 0.01f;
        }

        public static void NudgeInteractDistance(IVaultingRestrictions restrictions, ref float distance)
        {
            if (restrictions == null)
            {
                return;
            }

            var minDistantToInteract = restrictions.MinDistantToInteract;
            if (distance <= minDistantToInteract || !IsWithinInteractDistance(minDistantToInteract, distance))
            {
                return;
            }

            distance = minDistantToInteract;
        }

        public static bool TryMeasureObstacle(VaultingComponent component, out float distance)
        {
            distance = 0f;
            var obstacle = component?.VaultingModelDebug?.ObstacleCalculatorModelDebug;
            if (obstacle == null || obstacle.TargetCollider == null)
            {
                return false;
            }

            distance = obstacle.DistanceToMainObstacle;
            return true;
        }
    }
}
