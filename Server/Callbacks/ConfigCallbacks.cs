
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;

namespace MiyakoCarryService.Server.Callbacks;

[Injectable]
public sealed class ConfigCallbacks(
    ConfigService configService
)
{
    public ValueTask<string> HandleConfig(string url, IRequestData info, MongoId sessionId)
    {
        return new ValueTask<string>(GetConfig());
    }

    private string GetConfig()
    {
        var config = configService.GetConfig();
        var cfgStr = JsonSerializer.Serialize(config);
        var cfgObject = JsonNode.Parse(cfgStr)!.AsObject();
        return cfgObject.ToJsonString();
    }
}