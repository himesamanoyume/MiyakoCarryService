

using Fika.Core.Networking.LiteNetLib.Utils;
using UnityEngine;

namespace MiyakoCarryService.Fika.Packets
{
    public sealed class TalkMsgPacket : BasePacket
    {
        public EPhraseTrigger PhraseTrigger;
        public Vector3? Position;
        public string Key;

        public TalkMsgPacket()
        {
            
        }
        
        public override void Deserialize(NetDataReader reader)  
        {  
            base.Deserialize(reader);
            PhraseTrigger = reader.GetEnum<EPhraseTrigger>();
            Position = reader.GetNullableUnmanaged<Vector3>();
            Key = reader.GetString();
        }  
    
        public override void Serialize(NetDataWriter writer)  
        {  
            base.Serialize(writer);
            writer.PutEnum(PhraseTrigger);
            writer.PutNullableUnmanaged(Position);
            writer.Put(Key);
        } 
    }
}