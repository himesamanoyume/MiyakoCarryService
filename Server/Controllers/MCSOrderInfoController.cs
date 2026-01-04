
using System.Collections.Generic;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSOrderInfoController(
        MCSOrderInfoService mcsOrderInfoService
    )
    {
        public void AddOrderInfo(MCSOrderInfo orderInfo)
        {
            mcsOrderInfoService.AddOrderInfo(orderInfo);
        }

        public void AddOrderInfos(List<MCSOrderInfo> orderInfos)
        {
            mcsOrderInfoService.AddOrderInfos(orderInfos);
        }

        public void CreateOrderInfo(MongoId bossSessionId, int players, int carryServiceLevel, int duration, MongoId questId)
        {
            mcsOrderInfoService.CreateOrderInfo(bossSessionId, players, carryServiceLevel, duration, questId);
        }

        public void ProcessExpiredOrderInfos()
        {
            mcsOrderInfoService.ProcessExpiredOrderInfos();
        }

        public void RemoveOrderInfo(MCSOrderInfo orderInfo)
        {
            mcsOrderInfoService.RemoveOrderInfo(orderInfo);
        }

        public void SaveOrderInfo()
        {
            mcsOrderInfoService.SaveOrderInfo();
        }

        public List<MCSOrderInfo> GetAllOrderInfos()
        {
            return mcsOrderInfoService.GetAllOrderInfos();
        }

        public void SetOrderInfoStarted(MCSOrderInfo orderInfo, PmcData completeQuestPmcData)
        {
            mcsOrderInfoService.SetOrderInfoStarted(orderInfo, completeQuestPmcData);
        }

        public List<MCSOrderInfo> GetOrderInfos(MongoId sessionId)
        {
            return mcsOrderInfoService.GetOrderInfos(sessionId);
        }
    }
}