
using SPTarkov.Server.Core.Models.Common;
using MiyakoCarryService.Server.Models.Enums;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public abstract record BaseInfo
    {
        [JsonPropertyName("McsLeadPlayerId")]
        public required MongoId McsLeadPlayerId { get; set; }

        [JsonPropertyName("QuestId")]
        public required MongoId QuestId { get; set; }

        [JsonPropertyName("Status")]
        public required EInfoStatus Status { get; set; }

        [JsonPropertyName("ExpirationTime")]
        public required long ExpirationTime { get; set; }
    }
}