
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

        [JsonPropertyName("KeywordItemText")]
        public required string KeywordItemText { get; set; }

        [JsonPropertyName("LootingKeywordItem")]
        public required bool LootingKeywordItem { get; set; }

        [JsonPropertyName("LootingWishlistItem")]
        public required bool LootingWishlistItem { get; set; }

        [JsonPropertyName("LootingQuestItem")]
        public required bool LootingQuestItem { get; set; }

        [JsonPropertyName("BlockItemType")]
        public required int BlockItemType { get; set; }
    }
}