

using System.Collections.Generic;
using Fika.Core.Networking.LiteNetLib.Utils;
using UnityEngine;

namespace MiyakoCarryService.Fika.Packets
{
    public class TalkMsgPacket : BasePacket
    {
        public EPhraseTrigger PhraseTrigger;
        public Vector3? Position;
        public List<string> Keys = new(5);

        public TalkMsgPacket()
        {

        }

        public override void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            PhraseTrigger = reader.GetEnum<EPhraseTrigger>();
            Position = reader.GetNullableUnmanaged<Vector3>();
            Keys = new(5);
            var amount = reader.GetUShort();
            if (amount > 0)
            {
                for (var i = 0; i < amount; i++)
                {
                    Keys.Add(reader.GetString());
                }
            }
        }

        public override void Serialize(NetDataWriter writer)
        {
            base.Serialize(writer);
            writer.PutEnum(PhraseTrigger);
            writer.PutNullableUnmanaged(Position);
            writer.Put((ushort)Keys.Count);
            foreach (var key in Keys)
            {
                writer.Put(key, 0);
            }
        }
    }
}