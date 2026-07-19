
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record McsBotPlayerConfigRequestData : IRequestData
    {
        [JsonPropertyName("McsLeadPlayerId")]
        public required MongoId McsLeadPlayerId { get; set; }

        [JsonPropertyName("EnableLooting")]
        public required bool EnableLooting { get; set; }

        [JsonPropertyName("PriceThreshold")]
        public required int PriceThreshold { get; set; }

        [JsonPropertyName("KeywordItemText")]
        public required string KeywordItemText { get; set; }

        [JsonPropertyName("LootingKeywordItem")]
        public required bool LootingKeywordItem { get; set; }

        [JsonPropertyName("BlockItemType")]
        public required int BlockItemType { get; set; }

        [JsonPropertyName("FormationMatrix")]
        public required string FormationMatrix { get; set; }

        [JsonPropertyName("KeepFormation")]
        public required bool KeepFormation { get; set; }

        [JsonPropertyName("FormationSpacing")]
        public required float FormationSpacing { get; set; }
    }
}