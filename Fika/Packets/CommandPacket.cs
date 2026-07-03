

using Fika.Core.Networking.LiteNetLib.Utils;
using UnityEngine;

namespace MiyakoCarryService.Fika.Packets
{
    public class CommandPacket : BasePacket
    {
        public string CommandType;
        public Vector3? Position;
        public BodyPartType AimingBodyPartType;
        public string TargetId;

        public CommandPacket()
        {

        }

        public override void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            CommandType = reader.GetString();
            Position = reader.GetNullableUnmanaged<Vector3>();
            AimingBodyPartType = reader.GetEnum<BodyPartType>();
            TargetId = reader.GetString();
        }

        public override void Serialize(NetDataWriter writer)
        {
            base.Serialize(writer);
            writer.Put(CommandType, 0);
            writer.PutNullableUnmanaged(Position);
            writer.PutEnum(AimingBodyPartType);
            writer.Put(TargetId, 0);
        }
    }
}