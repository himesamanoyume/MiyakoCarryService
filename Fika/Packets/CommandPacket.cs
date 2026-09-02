
using Fika.Core.Networking.LiteNetLib.Utils;

namespace MiyakoCarryService.Fika.Packets
{
    public class CommandPacket : BasePacket
    {
        public string Payload;

        public override void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            Payload = reader.GetString();
        }

        public override void Serialize(NetDataWriter writer)
        {
            base.Serialize(writer);
            writer.Put(Payload, 0);
        }
    }
}