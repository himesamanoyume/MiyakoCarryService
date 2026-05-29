

using Fika.Core.Networking.LiteNetLib.Utils;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Fika.Packets
{
    public sealed class McsBotPlayerConfigPacket : BasePacket
    {
        public string KeywordItemText;
        public SMcsBotPlayerConfig McsBotPlayerConfig;

        public McsBotPlayerConfigPacket()
        {
            
        }
        
        public override void Deserialize(NetDataReader reader)  
        {  
            base.Deserialize(reader);
            KeywordItemText = reader.GetString();
            McsBotPlayerConfig = reader.GetUnmanaged<SMcsBotPlayerConfig>();
        }  
    
        public override void Serialize(NetDataWriter writer)  
        {  
            base.Serialize(writer);
            writer.Put(KeywordItemText, 0);
            writer.PutUnmanaged(McsBotPlayerConfig);
        } 
    }
}