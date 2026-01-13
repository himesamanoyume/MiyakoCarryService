
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace MiyakoCarryService.Server.Models.Eft.Bot
{
    public record SpawnCarryServiceBotRequestData
    {
        [JsonPropertyName("hostSessionId")]
        public MongoId hostSessionId { get; set; }
    }
}