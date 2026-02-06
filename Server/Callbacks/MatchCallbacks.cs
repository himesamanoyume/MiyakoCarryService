
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public sealed class MatchCallbacks(
        HttpResponseUtil httpResponseUtil,
        RaidController raidController
    )
    {
        /// <summary>
        /// 处理 /client/match/raid/ready
        /// </summary>
        public ValueTask<string> RaidReady(string url, EmptyRequestData _, MongoId mcsLeadPlayerId)
        {
            return new ValueTask<string>(httpResponseUtil.GetBody(true));
        }

        /// <summary>
        /// 处理 /client/match/raid/not-ready
        /// </summary>
        public ValueTask<string> NotRaidReady(string url, EmptyRequestData _, MongoId mcsLeadPlayerId)
        {
            return new ValueTask<string>(httpResponseUtil.GetBody(true));
        }

        /// <summary>
        /// 处理 /mcs/client/match/raid/abort
        /// </summary>
        public ValueTask<string> MatchingAbort(string url, EmptyRequestData _, MongoId mcsLeadPlayerId)
        {
            raidController.ClearGroupMember(mcsLeadPlayerId);
            return new ValueTask<string>(httpResponseUtil.GetBody(true));
        }
    }
}
