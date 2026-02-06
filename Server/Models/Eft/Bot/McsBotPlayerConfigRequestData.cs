
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record McsBotPlayerConfigRequestData : IRequestData
    {
        [JsonPropertyName("McsLeadPlayerId")]
        public required MongoId McsLeadPlayerId { get; set; }

        [JsonPropertyName("PriceThreshold")]
        public required int PriceThreshold { get; set; }

        [JsonPropertyName("ArmorLevelThreshold")]
        public required int ArmorLevelThreshold { get; set; }

        // // 子弹穿伤
        // [DataMember(Name = "BulletPenetrationThreshold")]
        // public int BulletPenetrationThreshold;

        // // 子弹肉伤
        // [DataMember(Name = "BulletDamageThreshold")]
        // public int BulletDamageThreshold;

        [JsonPropertyName("LootingWishlishItem")]
        public required bool LootingWishlishItem { get; set; }

        [JsonPropertyName("LootingQuestItem")]
        public required bool LootingQuestItem { get; set; }

        [JsonPropertyName("BlockItemType")]
        public required int BlockItemType { get; set; }
    }
}