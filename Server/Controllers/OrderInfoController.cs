
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;

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

        public void CreateOrderInfo(MongoId mcsLeadPlayerId, int players, int carryServiceLevel, int duration, MongoId questId)
        {
            orderInfoService.CreateOrderInfo(mcsLeadPlayerId, players, carryServiceLevel, duration, questId);
        }

        public ConcurrentDictionary<MongoId, HashSet<MongoId>> GetExpiredMcsBotPlayerIds()
        {
            return orderInfoService.GetExpiredMcsBotPlayerIds();
        }

        public void RemoveOrderInfo(OrderInfo orderInfo)
        {
            orderInfoService.RemoveOrderInfo(orderInfo);
        }

        public void CompleteOrderQuestSendFriendRequest(SptProfile mcsBotPlayerProfile, MongoId mcsLeadPlayerId)
        {
            orderInfoService.CompleteOrderQuestSendFriendRequest(mcsBotPlayerProfile, mcsLeadPlayerId);
        }

        public async Task SaveOrderInfo()
        {
            await orderInfoService.SaveOrderInfo();
        }

        public List<OrderInfo> GetAllOrderInfo()
        {
            return orderInfoService.GetAllOrderInfo();
        }

        public void SetOrderInfoStarted(OrderInfo orderInfo)
        {
            orderInfoService.SetOrderInfoStarted(orderInfo);
        }

        public List<OrderInfo> GetOrderInfos(MongoId sessionId)
        {
            return orderInfoService.GetOrderInfos(sessionId);
        }
    }
}