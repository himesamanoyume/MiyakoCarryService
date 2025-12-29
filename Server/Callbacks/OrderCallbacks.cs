
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Callbacks;

[Injectable]
public sealed class OrderCallbacks(
    HttpResponseUtil httpResponseUtil,
    OrderQuestController orderQuestController
)
{
    /// <summary>
    ///     Handle client/orderQuests/activityPeriods
    /// </summary>
    public ValueTask<string> ActivityPeriods(string url, EmptyRequestData _, MongoId sessionID)
    {
        return new ValueTask<string>(httpResponseUtil.GetBody(orderQuestController.GetClientOrderQuests(sessionID)));
    }
}