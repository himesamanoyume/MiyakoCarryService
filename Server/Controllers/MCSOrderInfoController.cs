

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Models.Enums;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSOrderInfoController(
        MCSOrderInfoService mcsOrderInfoService,
        MCSConfigService mcsConfigService,
        TimeUtil timeUtil
    )
    {
        public async Task CreateOrderInfo(MongoId sessionId, int players, int carryServiceLevel, int hours, MongoId questId)
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
            await mcsOrderInfoService.AddOrderInfo(sessionId, orderInfo);
        }

        public async Task ProcessExpiredOrderInfos(MongoId sessionId)
        {
            var orderInfos = await mcsOrderInfoService.GetAllOrderInfos(sessionId);
            List<MCSOrderInfo> keepOrderInfos = new();
            foreach (var orderInfo in orderInfos)
            {
                var currentTime = timeUtil.GetTimeStamp();
                if (currentTime < orderInfo.ExpirationTime - 1)
                {
                    keepOrderInfos.Add(orderInfo);
                }
            }
            await mcsOrderInfoService.SaveOrderInfo(sessionId, keepOrderInfos);
        }
    }
}