
using System.Collections.Generic;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class ConfigController(
        ConfigService configService,
        JsonUtil jsonUtil
    )
    {
        public string GetConfig()
        {
            var config = configService.GetMiyakoCarryServiceConfig();
            var cfgStr = jsonUtil.Serialize(config);
            return cfgStr;
        }

        public OrderConfig GetOrderConfig()
        {
            return configService.GetOrderConfig();
        }

        public string GetSpawnTypeDisplayName(string wildSpawnType)
        {
            return configService.GetSpawnTypeDisplayName(wildSpawnType);
        }

        public List<string> GetAllCustomBrainName()
        {
            return configService.GetAllCustomBrainName();
        }
    }
}