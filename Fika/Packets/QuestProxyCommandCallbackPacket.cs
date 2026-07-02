

using Fika.Core.Networking.LiteNetLib.Utils;

namespace MiyakoCarryService.Fika.Packets
{
    public class QuestProxyCommandCallbackPacket : BasePacket
    {
        public string TargetId;

        public QuestProxyCommandCallbackPacket()
        {

        }

        public override void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            TargetId = reader.GetString();
        }

        public override void Serialize(NetDataWriter writer)
        {
            base.Serialize(writer);
            writer.Put(TargetId, 0);
        }
    }
}