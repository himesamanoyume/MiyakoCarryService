
using System.Collections.Generic;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class OrderInfoController(
        OrderInfoService orderInfoService
    )
    {
        public void AddOrderInfo(OrderInfo orderInfo)
        {
            orderInfoService.AddOrderInfo(orderInfo);
        }

        public void AddOrderInfos(List<OrderInfo> orderInfos)
        {
            orderInfoService.AddOrderInfos(orderInfos);
        }

        public void CreateOrderInfo(MongoId mcsBossPlayerId, int players, int carryServiceLevel, int duration, MongoId questId)
        {
            orderInfoService.CreateOrderInfo(mcsBossPlayerId, players, carryServiceLevel, duration, questId);
        }

        public void ProcessExpiredOrderInfos()
        {
            orderInfoService.ProcessExpiredOrderInfos();
        }

        public void RemoveOrderInfo(OrderInfo orderInfo)
        {
            orderInfoService.RemoveOrderInfo(orderInfo);
        }

        public void SaveOrderInfo()
        {
            orderInfoService.SaveOrderInfo();
        }

        public List<OrderInfo> GetAllOrderInfos()
        {
            return orderInfoService.GetAllOrderInfos();
        }

        public void SetOrderInfoStarted(OrderInfo orderInfo, PmcData completeQuestPmcData)
        {
            orderInfoService.SetOrderInfoStarted(orderInfo, completeQuestPmcData);
        }

        public List<OrderInfo> GetOrderInfos(MongoId sessionId)
        {
            return orderInfoService.GetOrderInfos(sessionId);
        }
    }
}