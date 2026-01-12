
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public sealed class MatchCallbacks(
        HttpResponseUtil httpResponseUtil
    )
    {
        public ValueTask<string> RaidReady(string url, EmptyRequestData _, MongoId sessionID)
        {
            return new ValueTask<string>(httpResponseUtil.GetBody(true));
        }

        public ValueTask<string> NotRaidReady(string url, EmptyRequestData _, MongoId sessionID)
        {
            return new ValueTask<string>(httpResponseUtil.GetBody(true));
        }
    }
}
