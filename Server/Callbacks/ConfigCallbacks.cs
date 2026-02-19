
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public sealed class ConfigCallbacks(
        HttpResponseUtil httpResponseUtil,
        ConfigController configController
    )
    {
        /// <summary>
        /// 处理 XXX 还没做这个路由
        /// </summary>
        public async ValueTask<string> HandleConfig(string url, EmptyRequestData _, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.GetBody(configController.GetConfig());
        }
    }
}
