
using System.Text.Json;
using System.Text.Json.Nodes;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSConfigController(
        MCSConfigService mcsConfigService
    )
    {
        public string GetConfig()
        {
            var config = mcsConfigService.GetMiyakoCarryServiceConfig();
            var cfgStr = JsonSerializer.Serialize(config);
            var cfgObject = JsonNode.Parse(cfgStr)!.AsObject();
            return cfgObject.ToJsonString();
        }
    }
}