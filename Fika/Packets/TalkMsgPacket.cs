

using Fika.Core.Networking.LiteNetLib.Utils;
using UnityEngine;

namespace MiyakoCarryService.Fika.Packets
{
    public class TalkMsgPacket : BasePacket
    {
        public EPhraseTrigger PhraseTrigger;
        public Vector3? Position;

        public TalkMsgPacket()
        {
            
        }

        public TalkMsgPacket(EPhraseTrigger type)
        {
            PhraseTrigger = type;
        }
        
        public override void Deserialize(NetDataReader reader)  
        {  
            base.Deserialize(reader);
            PhraseTrigger = reader.GetEnum<EPhraseTrigger>();
            Position = reader.GetNullableUnmanaged<Vector3>();
        }  
    
        public override void Serialize(NetDataWriter writer)  
        {  
            base.Serialize(writer);
            writer.PutEnum(PhraseTrigger);
            writer.PutNullableUnmanaged(Position);
        } 
    }
}