

using Fika.Core.Networking.LiteNetLib.Utils;
using UnityEngine;

namespace MiyakoCarryService.Fika.Packets
{
    public class CommandPacket : QuestProxyCommandCallbackPacket
    {
        public string CommandType;
        public Vector3? Position;
        public BodyPartType AimingBodyPartType;

        public override void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            CommandType = reader.GetString();
            Position = reader.GetNullableUnmanaged<Vector3>();
            AimingBodyPartType = reader.GetEnum<BodyPartType>();
        }

        public override void Serialize(NetDataWriter writer)
        {
            base.Serialize(writer);
            writer.Put(CommandType, 0);
            writer.PutNullableUnmanaged(Position);
            writer.PutEnum(AimingBodyPartType);
        }
    }
}