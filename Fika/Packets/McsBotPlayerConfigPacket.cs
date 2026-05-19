

using Fika.Core.Networking.LiteNetLib.Utils;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Fika.Packets
{
    public sealed class McsBotPlayerConfigPacket : BasePacket
    {
        public SMcsBotPlayerConfig McsBotPlayerConfig;

        public McsBotPlayerConfigPacket()
        {
            
        }
        
        public override void Deserialize(NetDataReader reader)  
        {  
            base.Deserialize(reader);
            McsBotPlayerConfig = reader.GetUnmanaged<SMcsBotPlayerConfig>();
        }  
    
        public override void Serialize(NetDataWriter writer)  
        {  
            base.Serialize(writer);
            writer.PutUnmanaged(McsBotPlayerConfig);
        } 
    }
}