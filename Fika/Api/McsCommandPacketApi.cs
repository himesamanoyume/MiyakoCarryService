using System;
using Fika.Core.Main.Players;
using MiyakoCarryService.Fika.Packets;
using MiyakoCarryService.Fika.Utils;

namespace MiyakoCarryService.Fika.Api
{
    public static class McsCommandPacketApi
    {
        public static void RegisterHandleAction(string commandPacketTypeName, Action<CommandPacket, FikaPlayer, FikaPlayer> action)
        {
            CommandPacketUtils.RegisterHandleAction(commandPacketTypeName, action);
        }
    }
}