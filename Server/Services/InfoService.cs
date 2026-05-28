using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Models.Enums;
using MiyakoCarryService.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers.Ws;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class InfoService(
        ConfigService configService,
        NotificationSendHelper notificationSendHelper,
        ISptLogger<InfoService> logger,
        NotificationHelper notificationHelper,
        SptWebSocketConnectionHandler sptWebSocketConnectionHandler,
        ServerLocalisationService serverLocalisationService,
        TimeUtil timeUtil,
        JsonUtil jsonUtil,
        FileUtil fileUtil
    )
    {
        private readonly string _orderFolderDir = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "orders");

        // 此处的MongoId为QuestId，而不是玩家Id
        private readonly ConcurrentDictionary<MongoId, OrderInfo> _orderInfos = new();
        private readonly ConcurrentDictionary<MongoId, TicketInfo> _ticketInfos = new();
        // end
        private SemaphoreSlim _saveLock = new(1, 1);

        public void AddOrderInfo(OrderInfo orderInfo)
        {
            _orderInfos.TryAdd(orderInfo.QuestId, orderInfo);
        }

        public void AddTicketInfo(TicketInfo tickInfo)
        {
            _ticketInfos.TryAdd(tickInfo.QuestId, tickInfo);
        }

        public void AddOrderInfos(List<OrderInfo> orderInfos)
        {
            foreach (var orderInfo in orderInfos)
            {
                AddOrderInfo(orderInfo);
            }
        }

        public void AddTicketInfos(List<TicketInfo> tickInfos)
        {
            foreach (var tickInfo in tickInfos)
            {
                AddTicketInfo(tickInfo);
            }
        }

        public void RemoveOrderInfo(OrderInfo orderInfo)
        {
            _orderInfos.TryRemove(orderInfo.QuestId, out _);
        }

        public void RemoveTicketInfo(TicketInfo tickInfo)
        {
            _ticketInfos.TryRemove(tickInfo.QuestId, out _);
        }

        public bool CheckMcsBotPlayerExist(MongoId mcsBotPlayerId)
        {
            foreach (var orderInfo in _orderInfos.Values)
            {
                if (orderInfo.Status is not EInfoStatus.Started)
                {
                    continue;
                }
                
                if (orderInfo.PlayerIds.Contains(mcsBotPlayerId))
                {
                    return true;
                }
            }
            return false;
        }

        public void CreateOrderInfo(MongoId mcsLeadPlayerId, int players, SpawnType spawnType, int carryServiceLevel, int duration, MongoId questId)
        {
            var hashSetPlayers = new HashSet<MongoId>();
            for (int i = 0; i < players; i++)
            {
                hashSetPlayers.Add(new());
            }
            var orderInfo = new OrderInfo()
            {
                McsLeadPlayerId = mcsLeadPlayerId,
                QuestId = questId,
                PlayerIds = hashSetPlayers,
                SpawnType = spawnType,
                CarryServiceLevel = carryServiceLevel,
                Duration = duration,
                Status = EInfoStatus.AvailableForStart,
                ExpirationTime = timeUtil.GetTimeStamp() + configService.GetOrderConfig().OrderQuests.First().ResetTime
            };
            AddOrderInfo(orderInfo);
        }

        public void CreateTicketInfo(MongoId mcsLeadPlayerId, int percent, MongoId questId)
        {
            var orderInfo = new TicketInfo()
            {
                McsLeadPlayerId = mcsLeadPlayerId,
                QuestId = questId,
                Percent = percent,
                Status = EInfoStatus.AvailableForStart,
                ExpirationTime = timeUtil.GetTimeStamp() + configService.GetOrderConfig().OrderQuests.First().ResetTime
            };
            AddTicketInfo(orderInfo);
        }

        public async Task SaveOrderAndTicketInfo()
        {
            if (_saveLock is null)
            {
                _saveLock = new(1, 1);
            }
            
            await _saveLock.WaitAsync();
            try
            {
                try
                {
                    var orderPath = System.IO.Path.Combine(_orderFolderDir, "orderinfo.json");
                    var orderInfos = _orderInfos.Values.ToList();
                    var jsonOrderInfos = jsonUtil.Serialize(orderInfos, true);
                    await fileUtil.WriteFileAsync(orderPath, jsonOrderInfos);

                    var ticketPath = System.IO.Path.Combine(_orderFolderDir, "ticketinfo.json");
                    var ticketInfos = _ticketInfos.Values.ToList();
                    var jsonTicketInfos = jsonUtil.Serialize(ticketInfos, true);
                    await fileUtil.WriteFileAsync(ticketPath, jsonTicketInfos);
                }
                catch (Exception e)
                {
                    logger.Error(serverLocalisationService.GetText(Locales.SAVEORDERINFOEXCEPTION), e);
                }
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public List<OrderInfo> GetOrderInfos(MongoId mcsLeadPlayerId)
        {
            List<OrderInfo> targetOrderInfos = new();

            foreach (var orderInfo in _orderInfos.Values.ToList())
            {
                if (orderInfo.McsLeadPlayerId == mcsLeadPlayerId)
                {
                    targetOrderInfos.Add(orderInfo);
                }
            }

            return targetOrderInfos;
        }

        public List<TicketInfo> GetTicketInfos(MongoId mcsLeadPlayerId)
        {
            List<TicketInfo> targetTicketInfos = new();

            foreach (var ticketInfo in _ticketInfos.Values.ToList())
            {
                if (ticketInfo.McsLeadPlayerId == mcsLeadPlayerId)
                {
                    targetTicketInfos.Add(ticketInfo);
                }
            }

            return targetTicketInfos;
        }

        public List<OrderInfo> GetAllOrderInfo()
        {
            return _orderInfos.Values.ToList();
        }

        public List<TicketInfo> GetAllTicketInfo()
        {
            return _ticketInfos.Values.ToList();
        }

        public async Task LoadAllOrderInfos()
        {
            var orderPath = System.IO.Path.Combine(_orderFolderDir, "orderinfo.json");
            if (!fileUtil.FileExists(orderPath))
            {
                await fileUtil.WriteFileAsync(orderPath, "[]");
            }

            var orderInfos = await jsonUtil.DeserializeFromFileAsync<List<OrderInfo>>(orderPath);
            AddOrderInfos(orderInfos);
        }

        public async Task LoadAllTicketInfos()
        {
            var ticketPath = System.IO.Path.Combine(_orderFolderDir, "ticketinfo.json");
            if (!fileUtil.FileExists(ticketPath))
            {
                await fileUtil.WriteFileAsync(ticketPath, "[]");
            }

            var ticketInfos = await jsonUtil.DeserializeFromFileAsync<List<TicketInfo>>(ticketPath);
            AddTicketInfos(ticketInfos);
        }

        public ConcurrentDictionary<MongoId, HashSet<MongoId>> GetExpiredMcsBotPlayerIds()
        {
            var mcsBotPlayerIds = new ConcurrentDictionary<MongoId, HashSet<MongoId>>();
            var orderInfos = GetAllOrderInfo();

            foreach (var orderInfo in orderInfos)
            {
                var currentTime = timeUtil.GetTimeStamp();
                if (currentTime >= orderInfo.ExpirationTime - 1)
                {
                    if (orderInfo.Status == EInfoStatus.AvailableForStart)
                    {
                        continue;
                    }
                    
                    mcsBotPlayerIds.GetOrAdd(orderInfo.McsLeadPlayerId, _ => new());
                    foreach (var mcsBotPlayerId in orderInfo.PlayerIds)
                    {
                        mcsBotPlayerIds[orderInfo.McsLeadPlayerId].Add(mcsBotPlayerId);
                    }
                }
            }
            return mcsBotPlayerIds;
        }

        public void ProcessExpiredOrderAndTicketInfo(MongoId mcsLeadPlayerId)
        {
            var orderInfos = GetAllOrderInfo();
            foreach (var orderInfo in orderInfos)
            {
                var currentTime = timeUtil.GetTimeStamp();
                if (orderInfo.McsLeadPlayerId == mcsLeadPlayerId && currentTime >= orderInfo.ExpirationTime - 1)
                {
                    RemoveOrderInfo(orderInfo);
                }
            }
            var ticketInfos = GetAllTicketInfo();
            foreach (var ticketInfo in ticketInfos)
            {
                var currentTime = timeUtil.GetTimeStamp();
                if (ticketInfo.McsLeadPlayerId == mcsLeadPlayerId && currentTime >= ticketInfo.ExpirationTime - 1)
                {
                    RemoveTicketInfo(ticketInfo);
                }
            }
            _ = SaveOrderAndTicketInfo();
        }

        public ConcurrentDictionary<MongoId, HashSet<MongoId>> SetAllOrderInfosToExpire(MongoId mcsLeadPlayerId)
        {
            var mcsBotPlayerIds = new ConcurrentDictionary<MongoId, HashSet<MongoId>>();
            var orderInfos = GetAllOrderInfo();

            foreach (var orderInfo in orderInfos)
            {
                if (orderInfo.McsLeadPlayerId == mcsLeadPlayerId)
                {
                    RemoveOrderInfo(orderInfo);
                    mcsBotPlayerIds.GetOrAdd(orderInfo.McsLeadPlayerId, _ => new());
                    foreach (var mcsBotPlayerId in orderInfo.PlayerIds)
                    {
                        mcsBotPlayerIds[orderInfo.McsLeadPlayerId].Add(mcsBotPlayerId);
                    }
                }
            }
            _ = SaveOrderAndTicketInfo();
            return mcsBotPlayerIds;
        }

        public void SetBaseInfoStarted(BaseInfo baseInfo)
        {
            if (baseInfo.Status == EInfoStatus.AvailableForStart)
            {
                baseInfo.Status = EInfoStatus.Started;
                var currentTime = timeUtil.GetTimeStamp();
                if (baseInfo is OrderInfo orderInfo)
                {
                    orderInfo.ExpirationTime = currentTime + orderInfo.Duration * 3600;
                }
                else if (baseInfo is TicketInfo ticketInfo)
                {
                    ticketInfo.ExpirationTime = currentTime + 60 * 3600;
                }
            }
        }

        public void CompleteOrderQuestSendFriendRequest(SptProfile mcsBotPlayerProfile, MongoId mcsLeadPlayerId)
        {
            _ = new Timer(
                _ =>
                {
                    try
                    {
                        if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsLeadPlayerId))
                        {
                            var notification = notificationHelper.GenerateWsFriendsListAccept(mcsBotPlayerProfile, NotificationEventType.friendListRequestAccept);
                            notificationSendHelper.SendMessage(mcsLeadPlayerId, notification);
                        }
                    }
                    finally
                    {
                        
                    }
                },
                null,
                TimeSpan.FromMicroseconds(1000),
                Timeout.InfiniteTimeSpan
            );
        }

        public void WaivePunish()
        {
            
        }

        public async Task OnPostLoadAsync()
        {
            await LoadAllOrderInfos();
            await LoadAllTicketInfos();
        }
    }
}