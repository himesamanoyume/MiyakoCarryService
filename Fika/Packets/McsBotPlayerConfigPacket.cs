

using Fika.Core.Networking.LiteNetLib.Utils;

namespace MiyakoCarryService.Fika.Packets
{
    public sealed class McsBotPlayerConfigPacket : BasePacket
    {
        public int PriceThreshold;
        public int ArmorLevelThreshold;
        public bool LootingWishlishItem;
        public bool LootingQuestItem;
        public int BlockItemType;

        public McsBotPlayerConfigPacket()
        {
            
        }
        
        public override void Deserialize(NetDataReader reader)  
        {  
            base.Deserialize(reader);
            PriceThreshold = reader.GetInt();
            ArmorLevelThreshold = reader.GetInt();
            LootingWishlishItem = reader.GetBool();
            LootingQuestItem = reader.GetBool();
            BlockItemType = reader.GetInt();
        }  
    
        public override void Serialize(NetDataWriter writer)  
        {  
            base.Serialize(writer);
            writer.Put(PriceThreshold);
            writer.Put(ArmorLevelThreshold);
            writer.Put(LootingWishlishItem);
            writer.Put(LootingQuestItem);
            writer.Put(BlockItemType);
        } 
    }
}