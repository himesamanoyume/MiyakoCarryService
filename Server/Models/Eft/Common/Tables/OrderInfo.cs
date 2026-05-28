
using SPTarkov.Server.Core.Models.Common;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record OrderInfo : BaseInfo
    {
        [JsonPropertyName("PlayerIds")]
        public required HashSet<MongoId> PlayerIds { get; set; }

        [JsonPropertyName("SpawnType")]
        public required SpawnType SpawnType { get; set; }

        [JsonPropertyName("CarryServiceLevel")]
        public required int CarryServiceLevel { get; set; }

        [JsonPropertyName("Duration")]
        public required int Duration { get; set; }
    }
}