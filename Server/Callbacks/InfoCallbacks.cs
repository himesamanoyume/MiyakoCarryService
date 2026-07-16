using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public class InfoCallbacks(
        HttpResponseUtil httpResponseUtil,
        ProfileController profileController,
        QuestController questController
    )
    {
        /// <summary> 
        /// 处理 /mcs/client/order/settle 
        /// </summary>  
        public virtual async ValueTask<string> SettleOrder(string url, McsBotPlayerAidRequestData info, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.NoBody(profileController.SettleOrder(mcsLeadPlayerId, info.Aid));
        }

        /// <summary> 
        /// 处理 /mcs/client/order/renew 
        /// </summary>  
        public virtual async ValueTask<string> RenewOrder(string url, McsBotPlayerAidRequestData info, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.NoBody(questController.RenewOrder(mcsLeadPlayerId, info.Aid));
        }
    }
}