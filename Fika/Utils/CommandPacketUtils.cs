
using System;
using System.Collections.Concurrent;
using MiyakoCarryService.Fika.Packets;

namespace MiyakoCarryService.Fika.Utils
{
    public static class CommandPacketUtils
    {
        private static ConcurrentDictionary<string, Action<CommandPacket>> _handleActionsMap;
    }
}