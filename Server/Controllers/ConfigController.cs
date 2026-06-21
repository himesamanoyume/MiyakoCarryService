
using System.Collections.Generic;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class ConfigController(
        ConfigService configService
    )
    {
        public McsPluginConfig GetMcsPluginConfig()
        {
            return configService.GetMcsPluginConfig();
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