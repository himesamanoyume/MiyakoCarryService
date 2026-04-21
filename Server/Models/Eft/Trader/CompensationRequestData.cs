
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Trader
{
    public record CompensationRequestData : IRequestData
    {
        [JsonPropertyName("McsLeadPlayerId")]
        public required MongoId McsLeadPlayerId { get; set; }
    }
}