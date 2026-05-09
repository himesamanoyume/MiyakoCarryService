
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Callbacks
{
    [Injectable]
    public sealed class ProfileCallbacks(
        HttpResponseUtil httpResponseUtil,
        ProfileController profileController
    )
    {
        /// <summary>
        /// 处理 /mcs/client/game/profile/list
        /// </summary>
        public async ValueTask<string> GetMcsBotPlayerProfileForInventoryMode(string url, EmptyRequestData info, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.NoBody(profileController.GetMcsBotPlayerProfileForInventoryMode(mcsLeadPlayerId));
        }

        /// <summary>
        /// 处理 /mcs/client/game/aid/verify
        /// </summary>
        public async ValueTask<string> VerifyMcsBotPlayerAid(string url, McsBotPlayerAidRequestData info, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.NoBody(await profileController.VerifyMcsBotPlayerAid(mcsLeadPlayerId, info.Aid));
        }

        /// <summary>
        /// 处理 /mcs/client/game/aid/remove
        /// </summary>
        public async ValueTask<string> RemoveMcsBotPlayerAid(string url, McsBotPlayerAidRequestData info, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.NoBody(await profileController.RemoveMcsBotPlayerAid(mcsLeadPlayerId, info.Aid));
        }
    }
}