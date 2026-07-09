using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Ws;

namespace MiyakoCarryService.Server.Models.Eft.Ws
{
    public record McsWsGroupMatchInviteDecline : WsNotificationEvent
    {
        [JsonPropertyName("aid")]
        public int? Aid { get; set; }

        [JsonPropertyName("Nickname")]
        public string? Nickname { get; set; }
    }
}