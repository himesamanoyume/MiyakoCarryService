using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Client.Utils
{
    internal static class PlayerVaultHintRecorder
    {
        private static readonly Dictionary<MovementContext, VaultHint> _pending = new Dictionary<MovementContext, VaultHint>();
        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        public static void OnEnter(MovementContext movementContext)
        {
            if (!ShouldRecord(movementContext))
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

            if (!ShouldRecord(movementContext))
            {
                return;
            }

            hint.EndPos = movementContext.TransformPosition;

            if ((hint.EndPos - hint.StartPos).magnitude <= 0.5f)
            {
                return;
            }

            PlayerVaultHints.Add(hint);
            if (!Tools.IsHost)
            {
                McsEventApi.Notify(new PlayerVaultHintHandleFikaEvent
                {
                    Hint = hint
                });
            }
        }

        private static bool ShouldRecord(MovementContext movementContext)
        {
            var player = MovementContextUtils.GetPlayer(movementContext);
            if (player == null)
            {
                return false;
            }

            var botOwner = player.AIData?.BotOwner;
            if (botOwner == null)
            {
                return true;
            }

            return botOwner.IsMcsBotPlayer;
        }
    }
}
