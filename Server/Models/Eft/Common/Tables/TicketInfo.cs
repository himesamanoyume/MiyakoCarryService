
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record TicketInfo : BaseInfo
    {
        [JsonPropertyName("Percent")]
        public required int Percent { get; set; }
    }
}