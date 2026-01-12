
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public sealed class ConfigCallbacks(
        ConfigController configController
    )
    {
        public ValueTask<string> HandleConfig(string url, IRequestData info, MongoId sessionId)
        {
            return new ValueTask<string>(configController.GetConfig());
        }
    }
}
