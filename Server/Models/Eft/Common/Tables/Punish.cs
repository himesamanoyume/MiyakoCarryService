
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record Punish
    {
        [JsonPropertyName("PunishmentMulti")]
        public required double PunishmentMulti { get; set; }
    }
}