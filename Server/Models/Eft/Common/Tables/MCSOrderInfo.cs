
using SPTarkov.Server.Core.Models.Common;
using MiyakoCarryService.Server.Models.Enums;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record MCSOrderInfo
    {
        [JsonPropertyName("SessionID")]
        public MongoId SessionID { get; set; }

        [JsonPropertyName("CarryServiceLevel")]
        public int CarryServiceLevel { get; set; }

        [JsonPropertyName("EndTime")]
        public long EndTime { get; set; }

        [JsonPropertyName("Status")]
        public EOrderInfoStatus Status { get; set; }
    }
}