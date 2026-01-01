
using System.Collections.Generic;
using System.Linq;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Models.Enums;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSOrderInfoController(
        MCSOrderInfoService mcsOrderInfoService,
        MCSProfileController mcsProfileController, 
        MCSConfigService mcsConfigService,
        TimeUtil timeUtil
    )
    {
        public void CreateOrderInfo(MongoId sessionId, int players, int carryServiceLevel, int hours, MongoId questId)
        {
            var hashSetPlayers = new HashSet<MongoId>();
            for (int i = 0; i < players; i++)
            {
                hashSetPlayers.Add(new MongoId());
            }
            var orderInfo = new MCSOrderInfo()
            {
                SessionId = sessionId,
                QuestId = questId,
                PlayerIds = hashSetPlayers,
                CarryServiceLevel = carryServiceLevel,
                Duration = hours,
                Status = EOrderInfoStatus.AvailableForStart,
                ExpirationTime = timeUtil.GetTimeStamp() + mcsConfigService.GetOrderConfig().OrderQuests.First().ResetTime
            };
            mcsOrderInfoService.AddOrderInfo(orderInfo);
        }

        public void ProcessExpiredOrderInfos(PmcData pmcData)
        {
            var orderInfos = mcsOrderInfoService.GetAllOrderInfos();
            foreach (var orderInfo in orderInfos)
            {
                var currentTime = timeUtil.GetTimeStamp();
                if (currentTime >= orderInfo.ExpirationTime - 1)
                {
                    mcsOrderInfoService.RemoveOrderInfo(orderInfo);
                    foreach (var csPlayerSessionId in orderInfo.PlayerIds)
                    {
                        mcsProfileController.ProcessExpiredCarryServiceProfile(orderInfo.SessionId, csPlayerSessionId);
                    }
                }
            }
            mcsOrderInfoService.SaveOrderInfo();
        }
    }
}