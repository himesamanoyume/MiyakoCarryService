using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Client.Utils
{
    internal static class PlayerVaultHintRecorder
    {
        private static readonly Dictionary<MovementContext, VaultHint> _pending = new Dictionary<MovementContext, VaultHint>();

        public static void OnEnter(MovementContext movementContext)
        {
            if (movementContext == null)
            {
                return;
            }

            _pending[movementContext] = new VaultHint
            {
                StartPos = movementContext.TransformPosition,
                Forward = movementContext.PlayerRealForward,
            };
        }

        public static void OnExit(MovementContext movementContext)
        {
            if (movementContext == null)
            {
                return;
            }

            if (!_pending.TryGetValue(movementContext, out var hint))
            {
                return;
            }

            _pending.Remove(movementContext);

            hint.EndPos = movementContext.TransformPosition;

            if ((hint.EndPos - hint.StartPos).magnitude <= 0.3f)
            {
                return;
            }

            PlayerVaultHints.Add(hint);
        }
    }
}
