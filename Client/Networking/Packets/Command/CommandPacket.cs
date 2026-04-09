

using Fika.Core.Networking.LiteNetLib.Utils;
using MiyakoCarryService.Client.Enums;

namespace MiyakoCarryService.Client.Networking.Packets.Command
{
    public class CommandPacket : INetSerializable
    {
        public ECommandPacketType CommandType;
        public int McsLeadPlayerNetId;
        public int McsBotPlayerNetId;

        public CommandPacket()
        {
            
        }

        public CommandPacket(ECommandPacketType type)
        {
            CommandType = type;
        }
        
        public void Deserialize(NetDataReader reader)  
        {  
            CommandType = reader.GetEnum<ECommandPacketType>();
            McsLeadPlayerNetId = reader.GetInt();
            McsBotPlayerNetId = reader.GetInt();
        }  
    
        public void Serialize(NetDataWriter writer)  
        {  
            writer.PutEnum(CommandType);
            writer.Put(McsLeadPlayerNetId);  
            writer.Put(McsBotPlayerNetId);
        } 
    }
}