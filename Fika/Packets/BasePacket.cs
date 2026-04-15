

using Fika.Core.Networking.LiteNetLib.Utils;

namespace MiyakoCarryService.Fika.Packets
{
    public abstract class BasePacket : INetSerializable
    {
        public int McsLeadPlayerNetId;
        public int McsBotPlayerNetId;

        public virtual void Deserialize(NetDataReader reader)  
        {  
            McsLeadPlayerNetId = reader.GetInt();
            McsBotPlayerNetId = reader.GetInt();
        }  
    
        public virtual void Serialize(NetDataWriter writer)  
        {  
            writer.Put(McsLeadPlayerNetId);  
            writer.Put(McsBotPlayerNetId);
        } 
    }
}