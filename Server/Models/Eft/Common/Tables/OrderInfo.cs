
using SPTarkov.Server.Core.Models.Common;
using MiyakoCarryService.Server.Models.Enums;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record OrderInfo
    {
        [JsonPropertyName("McsLeadPlayerId")]
        public required MongoId McsLeadPlayerId { get; set; }

        [JsonPropertyName("QuestId")]
        public required MongoId QuestId { get; set; }

        [JsonPropertyName("PlayerIds")]
        public required HashSet<MongoId> PlayerIds { get; set; }

        [JsonPropertyName("CarryServiceLevel")]
        public required int CarryServiceLevel { get; set; }

        [JsonPropertyName("Duration")]
        public required int Duration { get; set; }

        [JsonPropertyName("Status")]
        public required EOrderInfoStatus Status { get; set; }

        [JsonPropertyName("ExpirationTime")]
        public required long ExpirationTime { get; set; }
    }
}