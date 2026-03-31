
using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record SpawnType
    {
        [JsonPropertyName("WildSpawnType")]
        public required string WildSpawnType { get; set; }

        [JsonPropertyName("IsBoss")]
        public required bool IsBoss { get; set; }

        [JsonPropertyName("DisplayName")]
        public required string DisplayName { get; set; }

        // 当前如果有使用特殊Brain的Mod的话Mcs无法将Layer添加至其中，因此客户端需要从服务端中收集带有自定义Brain的列表，然后在BrainMgr中对其添加（未实现）
        [JsonPropertyName("BrainName")]
        public string? BrainName { get; set; }
        
    }
}