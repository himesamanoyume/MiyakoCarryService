
using System;
using System.Collections.Concurrent;
using MiyakoCarryService.Fika.Packets;

namespace MiyakoCarryService.Fika.Utils
{
    internal static class CommandPacketUtils
    {
        private static ConcurrentDictionary<string, Action<CommandPacket>> _handleActionsMap;

        public static void RegisterHandleAction(string commandPacketTypeName, Action<CommandPacket> action)
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

        public static ConcurrentDictionary<string, Action<CommandPacket>> GetHandleActionsMap()
        {
            if (_handleActionsMap == null)
            {
                _handleActionsMap = new();
            }
            return _handleActionsMap;
        }
    }
}