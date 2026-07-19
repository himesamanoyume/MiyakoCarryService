

using System.Collections.Generic;
using Fika.Core.Networking.LiteNetLib.Utils;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Fika.Packets
{
    public class McsBotPlayerConfigPacket : BasePacket
    {
        public string KeywordItemText;
        public string FormationMatrix;
        public SMcsBotPlayerConfig McsBotPlayerConfig;
        public Dictionary<string, McsValue> Extensions;

        public override void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            KeywordItemText = reader.GetString();
            FormationMatrix = reader.GetString();
            McsBotPlayerConfig = reader.GetUnmanaged<SMcsBotPlayerConfig>();
            var count = reader.GetInt();
            Extensions = new();
            for (int i = 0; i < count; i++)
            {
                var key = reader.GetString();
                var type = (EMcsValueType)reader.GetByte();
                var v = new McsValue { Type = type };
                switch (type)
                {
                    case EMcsValueType.Bool:
                        v.BoolValue = reader.GetBool();
                        break;
                    case EMcsValueType.Int:
                    case EMcsValueType.Long:
                    case EMcsValueType.Enum:
                        v.IntValue = reader.GetLong();
                        break;
                    case EMcsValueType.Float:
                        v.FloatValue = reader.GetFloat();
                        break;
                    case EMcsValueType.String:
                        v.StringValue = reader.GetString();
                        break;
                }
                Extensions[key] = v;
            }
        }

        public override void Serialize(NetDataWriter writer)
        {
            base.Serialize(writer);
            writer.Put(KeywordItemText, 0);
            writer.Put(FormationMatrix, 0);
            writer.PutUnmanaged(McsBotPlayerConfig);
            writer.Put(Extensions.Count);
            foreach (var kv in Extensions)
            {
                writer.Put(kv.Key, 0);
                writer.Put((byte)kv.Value.Type);
                switch (kv.Value.Type)
                {
                    case EMcsValueType.Bool:
                        writer.Put(kv.Value.BoolValue);
                        break;
                    case EMcsValueType.Int:
                    case EMcsValueType.Long:
                    case EMcsValueType.Enum:
                        writer.Put(kv.Value.IntValue);
                        break;
                    case EMcsValueType.Float:
                        writer.Put(kv.Value.FloatValue);
                        break;
                    case EMcsValueType.String:
                        writer.Put(kv.Value.StringValue, 0);
                        break;
                }
            }
        }
    }
}