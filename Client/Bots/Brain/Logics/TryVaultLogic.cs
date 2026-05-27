
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    /// <summary>
    /// 借鉴SAIN实现AI进行翻越
    /// </summary>
    public sealed class TryVaultLogic : McsBotBaseLogic
    {
        private float _lastVaultCheckTime = 0f;
        private const float VAULT_CHECK_INTERVAL = 0.5f;
        private const float VAULT_HEIGHT_THRESHOLD = 1.5f;
        private const float SPHERECAST_RADIUS = 0.1f;
        private const float SPHERECAST_DISTANCE = 2f;
        private const float DIRECTION_ALIGNMENT_THRESHOLD = 0.85f;

        public TryVaultLogic(BotOwner botOwner) : base(botOwner)
        {

        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            if (_lastVaultCheckTime < Time.time)
            {
                _lastVaultCheckTime = Time.time + VAULT_CHECK_INTERVAL;

                if (ShouldTryVault())
                {
                    TryVault();
                }
            }
        }

        private bool ShouldTryVault()
        {
            if (BotOwner.GetPlayer == null || BotOwner.GetPlayer.VaultingComponent == null || BotOwner.GetPlayer.VaultingGameplayRestrictions == null)
            {
                return false;
            }

            if (!BotOwner.GetPlayer.VaultingGameplayRestrictions.CanVaulting())
            {
                return false;
            }

            if (!BotOwner.Mover.IsMoving)
            {
                return false;
            }

            var lookDirection = BotOwner.GetPlayer.LookDirection.normalized;
            var targetDirection = BotOwner.Mover.NormDirCurPoint;
            if (Vector3.Dot(lookDirection, targetDirection) < DIRECTION_ALIGNMENT_THRESHOLD)
            {
                return false;
            }

            if (Time.time - BotOwner.Mover.LastTimePosChanged < 3f)
            {
                return false;
            }

            return true;
        }

        private void TryVault()
        {
            if (CheckForVaultableObstacle())
            {
                if (BotOwner.GetPlayer.VaultingComponent.TryVaulting())
                {
                    BotOwner.GetPlayer.OnVaulting();
                }
            }
        }

        private bool CheckForVaultableObstacle()
        {
            var startPosition = BotOwner.GetPlayer.WeaponRoot.position;
            var lookDirection = BotOwner.GetPlayer.LookDirection.normalized;
            var endPosition = startPosition + lookDirection * SPHERECAST_DISTANCE;

            startPosition.y += 0.33f;
            endPosition.y += 0.33f;

            if (Physics.SphereCast(startPosition, SPHERECAST_RADIUS, lookDirection, out RaycastHit hit, SPHERECAST_DISTANCE, LayerMaskClass.PlayerStaticCollisionsMask))
            {
                if (hit.collider != null)
                {
                    float obstacleHeight = hit.collider.bounds.size.y;
                    float maxVaultHeight = BotOwner.GetPlayer.VaultingParameters.VaultingHeight;

                    return obstacleHeight < maxVaultHeight && obstacleHeight < VAULT_HEIGHT_THRESHOLD;
                }
            }

            return false;
        }
    }
}