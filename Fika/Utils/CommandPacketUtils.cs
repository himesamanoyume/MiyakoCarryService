
using System;
using System.Collections.Concurrent;
using Fika.Core.Main.Players;
using MiyakoCarryService.Fika.Packets;

namespace MiyakoCarryService.Fika.Utils
{
    internal static class CommandPacketUtils
    {
        private static ConcurrentDictionary<string, Action<CommandPacket, FikaPlayer, FikaPlayer>> _handleActionsMap;

        public static void RegisterHandleAction(string commandPacketTypeName, Action<CommandPacket, FikaPlayer, FikaPlayer> action)
        {
            if (_handleActionsMap == null)
            {
                _handleActionsMap = new();
            }

            _handleActionsMap.AddOrUpdate(commandPacketTypeName, action, 
                (commandPacketTypeName, oldAction) =>
                {
                    oldAction = action;
                    return oldAction;
                }
            );
        }

        public static Action<CommandPacket, FikaPlayer, FikaPlayer> GetHandleAction(string commandType)
        {
            if (_handleActionsMap == null)
            {
                _handleActionsMap = new();
            }

            _handleActionsMap.TryGetValue(commandType, out var action);
            return action;
        }
    }
}