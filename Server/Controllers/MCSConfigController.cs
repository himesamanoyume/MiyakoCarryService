
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSConfigController(
        MCSConfigService mcsConfigService,
        JsonUtil jsonUtil
    )
    {
        public string GetConfig()
        {
            var config = mcsConfigService.GetMiyakoCarryServiceConfig();
            var cfgStr = jsonUtil.Serialize(config);
            return cfgStr;
        }

        public MCSOrderConfig GetOrderConfig()
        {
            return mcsConfigService.GetOrderConfig();
        }
    }
}