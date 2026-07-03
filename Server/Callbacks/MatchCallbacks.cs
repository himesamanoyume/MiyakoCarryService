
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public class MatchCallbacks(
        HttpResponseUtil httpResponseUtil,
        RaidController raidController
    )
    {
        /// <summary>
        /// 处理 /client/match/raid/ready
        /// 因为SPT的此路由为 /client/match/group/raid/ready 已过时
        /// </summary>
        public virtual async ValueTask<string> RaidReady(string url, EmptyRequestData _, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.GetBody(true);
        }

        /// <summary>
        /// 处理 /client/match/raid/not-ready
        /// 因为SPT的此路由为 /client/match/group/raid/not-ready 已过时
        /// </summary>
        public virtual async ValueTask<string> NotRaidReady(string url, EmptyRequestData _, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.GetBody(true);
        }

        /// <summary>
        /// 处理 /mcs/client/match/raid/abort
        /// </summary>
        public virtual async ValueTask<string> MatchingAbort(string url, EmptyRequestData _, MongoId mcsLeadPlayerId)
        {
            raidController.ClearGroupMember(mcsLeadPlayerId);
            return httpResponseUtil.GetBody(true);
        }

        /// <summary>
        /// 处理 /mcs/client/match/group/delete
        /// 因为SPT的路由 /client/match/group/delete 已过时
        /// </summary>
        public virtual async ValueTask<string> DeleteGroup(string url, EmptyRequestData _, MongoId mcsLeadPlayerId)
        {
            raidController.ClearGroupMember(mcsLeadPlayerId);
            return httpResponseUtil.GetBody(true);
        }
    }
}
