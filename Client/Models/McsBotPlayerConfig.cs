
using System.Runtime.Serialization;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class McsBotPlayerConfig
    {
        [DataMember(Name = "McsBossPlayerId")]
        public MongoID McsBossPlayerId;

        [DataMember(Name = "PriceThreshold")]
        public int PriceThreshold;

        [DataMember(Name = "ArmorLevelThreshold")]
        public int ArmorLevelThreshold;

        // // 子弹穿伤
        // [DataMember(Name = "BulletPenetrationThreshold")]
        // public int BulletPenetrationThreshold;
        
        // // 子弹肉伤
        // [DataMember(Name = "BulletDamageThreshold")]
        // public int BulletDamageThreshold;

        [DataMember(Name = "LootingWishlishItem")]
        public bool LootingWishlishItem;

        [DataMember(Name = "LootingQuestItem")]
        public bool LootingQuestItem;

        [DataMember(Name = "BlockItemType")]
        public int BlockItemType;
    }
}