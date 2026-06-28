using System.Text.Json.Serialization;
using MiyakoCarryService.Server.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Eft.Ws;

namespace MiyakoCarryService.Server.Models.Eft.Ws
{
    public record McsWsGroupMatchRaidReady : WsNotificationEvent
    {
        [JsonPropertyName("extendedProfile")]
        public McsGroupCharacter? ExtendedProfile { get; set; }
    }
}