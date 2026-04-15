

using Fika.Core.Networking.LiteNetLib.Utils;
using MiyakoCarryService.Client.Enums;
using UnityEngine;

namespace MiyakoCarryService.Fika.Packets
{
    public class TalkMsgPacket : BasePacket
    {
        public ETalkContentType TalkContentType;
        public Vector3? Position;

        public TalkMsgPacket()
        {
            
        }

        public TalkMsgPacket(ETalkContentType type)
        {
            TalkContentType = type;
        }
        
        public override void Deserialize(NetDataReader reader)  
        {  
            base.Deserialize(reader);
            TalkContentType = reader.GetEnum<ETalkContentType>();
            Position = reader.GetNullableUnmanaged<Vector3>();
        }  
    
        public override void Serialize(NetDataWriter writer)  
        {  
            base.Serialize(writer);
            writer.PutEnum(TalkContentType);
            writer.PutNullableUnmanaged(Position);
        } 
    }
}