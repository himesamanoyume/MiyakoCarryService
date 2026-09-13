using EFT;
using EFT.Vaulting;
using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    public static class VaultAim
    {
        public static void FaceYaw(BotOwner botOwner, Vector3 direction)
        {
            var player = botOwner.GetPlayer;
            direction.y = 0f;
            if (player == null || direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var deltaYaw = Mathf.DeltaAngle(player.Rotation.x, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
            if (Mathf.Abs(deltaYaw) < 1f)
            {
                return;
            }

            player.Rotate(new Vector2(deltaYaw, 0f), true);
        }

        public static bool TryAlign(BotOwner botOwner, VaultingComponent component, Vector3 direction)
        {
            var bodyOff = MeasureBodyOffAngle(botOwner, direction);
            var gridOff = MeasureGridOffAngle(component);
            if (bodyOff <= 15f && gridOff <= 15f)
            {
                return true;
            }

            FaceYaw(botOwner, direction);
            component?.Tick();
            return MeasureBodyOffAngle(botOwner, direction) <= 15f && MeasureGridOffAngle(component) <= 15f;
        }

        private static float MeasureBodyOffAngle(BotOwner botOwner, Vector3 direction)
        {
            var player = botOwner.GetPlayer;
            direction.y = 0f;
            if (player == null || direction.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            var bodyForward = player.MovementContext.PlayerRealForward;
            bodyForward.y = 0f;
            return Vector3.Angle(bodyForward, direction);
        }

        private static float MeasureGridOffAngle(VaultingComponent component)
        {
            var gridRoot = component?._vaultingContext?.VaultingGridRoot;
            if (gridRoot == null)
            {
                return 0f;
            }

            var gridForward = gridRoot.forward;
            gridForward.y = 0f;
            var bodyForward = component._vaultingContext.PlayerRealForward;
            bodyForward.y = 0f;
            return Vector3.Angle(gridForward, bodyForward);
        }
    }
}
