
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
        public ConcurrentDictionary<MongoId, HashSet<MongoId>> GetExpiredMcsBotPlayerIds()
        {
            return orderInfoService.GetExpiredMcsBotPlayerIds();
        }

        public void ProcessExpiredOrderInfo(MongoId mcsLeadPlayerId)
        {
            orderInfoService.ProcessExpiredOrderInfo(mcsLeadPlayerId);
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

        public List<OrderInfo> GetOrderInfos(MongoId mcsLeadPlayerId)
        {
            return orderInfoService.GetOrderInfos(mcsLeadPlayerId);
        }

        public bool CheckMcsBotPlayerExist(MongoId mcsLeadPlayerId)
        {
            return orderInfoService.CheckMcsBotPlayerExist(mcsLeadPlayerId);
        }
    }
}