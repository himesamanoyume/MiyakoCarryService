
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public class ConfigCallbacks(
        HttpResponseUtil httpResponseUtil,
        ConfigController configController
    )
    {
        /// <summary>
        /// 处理 /mcs/client/config
        /// </summary>
        public virtual async ValueTask<string> GetMcsPluginClientConfig(string url, EmptyRequestData _, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.NoBody(configController.GetMcsPluginClientConfig());
        }
    }
}
