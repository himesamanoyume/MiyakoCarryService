

using System.Collections.Generic;
using Fika.Core.Networking.LiteNetLib.Utils;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Fika.Packets
{
    public class McsBotPlayerConfigPacket : BasePacket
    {
        public string KeywordItemText;
        public SMcsBotPlayerConfig McsBotPlayerConfig;
        public Dictionary<string, McsConfigValue> Extensions;

        public McsBotPlayerConfigPacket()
        {

        }

        public override void Serialize(NetDataWriter writer)
        {
            base.Serialize(writer);
            writer.Put(KeywordItemText, 0);
            writer.PutUnmanaged(McsBotPlayerConfig);
            writer.Put(Extensions.Count);
            foreach (var kv in Extensions)
            {
                writer.Put(kv.Key);
                writer.Put((byte)kv.Value.Type);
                switch (kv.Value.Type)
                {
                    case EMcsConfigValueType.Bool: 
                        writer.Put(kv.Value.BoolValue); 
                        break;
                    case EMcsConfigValueType.Int:
                    case EMcsConfigValueType.Long:
                    case EMcsConfigValueType.Enum: 
                        writer.Put(kv.Value.IntValue); 
                        break;
                    case EMcsConfigValueType.Float: 
                        writer.Put(kv.Value.FloatValue); 
                        break;
                    case EMcsConfigValueType.String: 
                        writer.Put(kv.Value.StringValue, 0); 
                        break;
                }
            }
        }

        public override void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            KeywordItemText = reader.GetString();
            McsBotPlayerConfig = reader.GetUnmanaged<SMcsBotPlayerConfig>();
            var count = reader.GetInt();
            Extensions = new();
            for (int i = 0; i < count; i++)
            {
                var key = reader.GetString();
                var type = (EMcsConfigValueType)reader.GetByte();
                var v = new McsConfigValue { Type = type };
                switch (type)
                {
                    case EMcsConfigValueType.Bool: 
                        v.BoolValue = reader.GetBool(); 
                        break;
                    case EMcsConfigValueType.Int:
                    case EMcsConfigValueType.Long:
                    case EMcsConfigValueType.Enum: 
                        v.IntValue = reader.GetLong(); 
                        break;
                    case EMcsConfigValueType.Float: 
                        v.FloatValue = reader.GetFloat(); 
                        break;
                    case EMcsConfigValueType.String: 
                        v.StringValue = reader.GetString(); 
                        break;
                }
                Extensions[key] = v;
            }
        }
    }
}