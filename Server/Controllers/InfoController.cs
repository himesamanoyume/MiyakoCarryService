
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
    public class InfoController(
        InfoService infoService
    )
    {
        public ConcurrentDictionary<MongoId, HashSet<MongoId>> GetExpiredMcsBotPlayerIds()
        {
            return infoService.GetExpiredMcsBotPlayerIds();
        }

        public void ProcessExpiredOrderInfo(MongoId mcsLeadPlayerId)
        {
            infoService.ProcessExpiredOrderAndTicketInfo(mcsLeadPlayerId);
        }

        public void RemoveOrderInfo(OrderInfo orderInfo)
        {
            infoService.RemoveOrderInfo(orderInfo);
        }
        
        public void RemoveTicketInfo(TicketInfo ticketInfo)
        {
            infoService.RemoveTicketInfo(ticketInfo);
        }

        public void CompleteOrderQuestSendFriendRequest(SptProfile mcsBotPlayerProfile, MongoId mcsLeadPlayerId)
        {
            infoService.CompleteOrderQuestSendFriendRequest(mcsBotPlayerProfile, mcsLeadPlayerId);
        }

        public async Task SaveOrderAndTicketInfo()
        {
            await infoService.SaveOrderAndTicketInfo();
        }

        public List<OrderInfo> GetAllOrderInfo()
        {
            return infoService.GetAllOrderInfo();
        }

        public List<TicketInfo> GetAllTicketInfo()
        {
            return infoService.GetAllTicketInfo();
        }

        public void SetBaseInfoStarted(BaseInfo baseInfo)
        {
            infoService.SetBaseInfoStarted(baseInfo);
        }

        public List<OrderInfo> GetOrderInfos(MongoId mcsLeadPlayerId)
        {
            return infoService.GetOrderInfos(mcsLeadPlayerId);
        }

        public List<TicketInfo> GetTicketInfos(MongoId mcsLeadPlayerId)
        {
            return infoService.GetTicketInfos(mcsLeadPlayerId);
        }

        public bool CheckMcsBotPlayerExist(MongoId mcsLeadPlayerId)
        {
            return infoService.CheckMcsBotPlayerExist(mcsLeadPlayerId);
        }
    }
}