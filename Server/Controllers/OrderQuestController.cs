
using System.Collections.Frozen;
using System.Collections.Generic;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class OrderQuestController(
        ProfileHelper profileHelper,
        TimeUtil timeUtil,
        ConfigService configService
        )
    {
        protected static readonly FrozenSet<string> _questTypes = ["PickUp"];
        protected readonly OrderConfig OrderConfig = configService.GetOrderConfig();

        public List<PmcDataRepeatableQuest> GetClientOrderQuests(MongoId sessionID)
        {
            // 参考 GetClientRepeatableQuests
            var returnData = new List<PmcDataRepeatableQuest>();
            var fullProfile = profileHelper.GetFullProfile(sessionID);
            var pmcData = fullProfile.CharacterData.PmcData;
            var currentTime = timeUtil.GetTimeStamp();

            var orderConfig = OrderConfig.OrderQuests;


            return returnData;
        }
    }
}