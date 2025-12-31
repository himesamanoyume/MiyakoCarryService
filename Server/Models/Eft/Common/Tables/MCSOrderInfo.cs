
using SPTarkov.Server.Core.Models.Common;
using MiyakoCarryService.Server.Models.Enums;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record MCSOrderInfo
    {
        [JsonPropertyName("SessionId")]
        public MongoId SessionId { get; set; }

        [JsonPropertyName("QuestId")]
        public MongoId QuestId { get; set; }

        [JsonPropertyName("PlayerIds")]
        public HashSet<MongoId> PlayerIds { get; set; }

        [JsonPropertyName("CarryServiceLevel")]
        public int CarryServiceLevel { get; set; }

        [JsonPropertyName("Duration")]
        public int Duration { get; set; }

        [JsonPropertyName("Status")]
        public EOrderInfoStatus Status { get; set; }

        [JsonPropertyName("ExpirationTime")]
        public long ExpirationTime { get; set; }
    }
}