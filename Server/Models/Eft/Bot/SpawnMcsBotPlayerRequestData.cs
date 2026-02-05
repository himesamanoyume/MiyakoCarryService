
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record SpawnMcsBotPlayerTypeRequestData : IRequestData
    {
        [JsonPropertyName("Side")]
        public required SideType Side { get; set; }
    }
}