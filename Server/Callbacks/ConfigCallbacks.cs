
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public sealed class ConfigCallbacks(
        ConfigController configController
    )
    {
        /// <summary>
        /// 处理 居然还没做这个路由
        /// </summary>
        public ValueTask<string> HandleConfig(string url, EmptyRequestData _, MongoId mcsBossPlayerId)
        {
            return new ValueTask<string>(configController.GetConfig());
        }
    }
}
