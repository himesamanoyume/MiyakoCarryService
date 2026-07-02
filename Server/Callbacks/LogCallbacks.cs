
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public class LogCallbacks(
        HttpResponseUtil httpResponseUtil,
        ISptLogger<LogCallbacks> logger
    )
    {
        /// <summary>
        /// 处理 /mcs/client/log
        /// </summary>
        public async ValueTask<string> PrintLog(string url, DebugRequestData info, MongoId mcsLeadPlayerId)
        {
            logger.Warning("[Mcs-Debug] " + info.Info);
            return httpResponseUtil.NullResponse();
        }
    }
}