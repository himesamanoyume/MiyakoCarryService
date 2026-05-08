
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record McsBotPlayerAidRequestData : IRequestData
    {
        [JsonPropertyName("Aid")]
        public required string Aid { get; set; }
    }
}