

using System.Collections.Generic;
using Fika.Core.Networking.LiteNetLib.Utils;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Models;
using UnityEngine;

namespace MiyakoCarryService.Fika.Packets
{
    public class CommandPacket : QuestProxyCommandCallbackPacket
    {
        public string CommandType;
        public Vector3? Position;
        public BodyPartType AimingBodyPartType;
        public Dictionary<string, McsValue> Extensions;

        public override void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            CommandType = reader.GetString();
            Position = reader.GetNullableUnmanaged<Vector3>();
            AimingBodyPartType = reader.GetEnum<BodyPartType>();
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
            writer.Put(CommandType, 0);
            writer.PutNullableUnmanaged(Position);
            writer.PutEnum(AimingBodyPartType);
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