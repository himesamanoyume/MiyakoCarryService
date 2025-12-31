
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public sealed class MCSConfigCallbacks(
        MCSConfigController mcsConfigController
    )
    {
        public ValueTask<string> HandleConfig(string url, IRequestData info, MongoId sessionId)
        {
            return new ValueTask<string>(mcsConfigController.GetConfig());
        }
    }
}
