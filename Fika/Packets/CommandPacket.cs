

using Fika.Core.Networking.LiteNetLib.Utils;
using MiyakoCarryService.Client.Enums;
using UnityEngine;

namespace MiyakoCarryService.Fika.Packets
{
    public sealed class CommandPacket : BasePacket
    {
        public ECommandPacketType CommandType;
        public Vector3? Position;
        public string TargetId;

        public CommandPacket()
        {

        }

        public override void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            CommandType = reader.GetEnum<ECommandPacketType>();
            Position = reader.GetNullableUnmanaged<Vector3>();
            TargetId = reader.GetString();
        }

        public override void Serialize(NetDataWriter writer)
        {
            base.Serialize(writer);
            writer.PutEnum(CommandType);
            writer.PutNullableUnmanaged(Position);
            writer.Put(TargetId, 0);
        }
    }
}