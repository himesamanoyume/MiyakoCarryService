using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Models.Enums;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers.Ws;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class OrderInfoService(
        ConfigService configService,
        NotificationSendHelper notificationSendHelper,
        ISptLogger<OrderInfoService> logger,
        NotificationHelper notificationHelper,
        SptWebSocketConnectionHandler sptWebSocketConnectionHandler,
        TimeUtil timeUtil,
        JsonUtil jsonUtil,
        FileUtil fileUtil
    )
    {
        private readonly string _orderFolderDir = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "orders");

        // 此处的MongoId为QuestId，而不是玩家Id
        private readonly ConcurrentDictionary<MongoId, OrderInfo> _orderInfos = new();
        private SemaphoreSlim _saveLock = new(1, 1);

        public void AddOrderInfo(OrderInfo orderInfo)
        {
            _orderInfos.TryAdd(orderInfo.QuestId, orderInfo);
        }

        public void AddOrderInfos(List<OrderInfo> orderInfos)
        {
            foreach (var orderInfo in orderInfos)
            {
                AddOrderInfo(orderInfo);
            }
        }

        public void RemoveOrderInfo(OrderInfo orderInfo)
        {
            _orderInfos.TryRemove(orderInfo.QuestId, out _);
        }

        public bool CheckMcsBotPlayerExist(MongoId mcsBotPlayerId)
        {
            foreach (var orderInfo in _orderInfos.Values)
            {
                if (orderInfo.PlayerIds.Contains(mcsBotPlayerId))
                {
                    return true;
                }
            }
            return false;
        }

        public void CreateOrderInfo(MongoId mcsBossPlayerId, int players, int carryServiceLevel, int duration, MongoId questId)
        {
            var hashSetPlayers = new HashSet<MongoId>();
            for (int i = 0; i < players; i++)
            {
                hashSetPlayers.Add(new MongoId());
            }
            var orderInfo = new OrderInfo()
            {
                McsBossPlayerId = mcsBossPlayerId,
                QuestId = questId,
                PlayerIds = hashSetPlayers,
                CarryServiceLevel = carryServiceLevel,
                Duration = duration,
                Status = EOrderInfoStatus.AvailableForStart,
                ExpirationTime = timeUtil.GetTimeStamp() + configService.GetOrderConfig().OrderQuests.First().ResetTime
            };
            AddOrderInfo(orderInfo);
        }

        public async Task SaveOrderInfo()
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
                }
                catch (Exception e)
                {
                    logger.Error("保存订单信息异常", e);
                }
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public List<OrderInfo> GetOrderInfos(MongoId mcsBossPlayerId)
        {
            List<OrderInfo> targetOrderInfos = new();

            foreach (var orderInfo in _orderInfos.Values.ToList())
            {
                if (orderInfo.McsBossPlayerId == mcsBossPlayerId)
                {
                    targetOrderInfos.Add(orderInfo);
                }
            }

            return targetOrderInfos;
        }

        public List<OrderInfo> GetAllOrderInfo()
        {
            return _orderInfos.Values.ToList();
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

        public ConcurrentDictionary<MongoId, HashSet<MongoId>> GetExpiredMcsBotPlayerIds()
        {
            var mcsBotPlayerIds = new ConcurrentDictionary<MongoId, HashSet<MongoId>>();
            var orderInfos = GetAllOrderInfo();

            foreach (var orderInfo in orderInfos)
            {
                var currentTime = timeUtil.GetTimeStamp();
                if (currentTime >= orderInfo.ExpirationTime - 1)
                {
                    logger.Info($"准备清除 {orderInfo.McsBossPlayerId} 的一个过期订单");
                    RemoveOrderInfo(orderInfo);
                    mcsBotPlayerIds.GetOrAdd(orderInfo.McsBossPlayerId, _ => new());
                    foreach (var mcsBotPlayerId in orderInfo.PlayerIds)
                    {
                        logger.Info($"准备清除 {mcsBotPlayerId} 的Profile");
                        mcsBotPlayerIds[orderInfo.McsBossPlayerId].Add(mcsBotPlayerId);
                    }
                }
            }
            _ = SaveOrderInfo();
            return mcsBotPlayerIds;
        }

        public void SetOrderInfoStarted(OrderInfo orderInfo)
        {
            if (orderInfo.Status == EOrderInfoStatus.AvailableForStart)
            {
                orderInfo.Status = EOrderInfoStatus.Started;
                var currentTime = timeUtil.GetTimeStamp();
                orderInfo.ExpirationTime = currentTime + orderInfo.Duration * 60; // 记得改回3600
            }
        }

        public void CompleteOrderQuestSendFriendRequest(SptProfile mcsBotPlayerProfile, MongoId mcsBossPlayerId)
        {
            _ = new Timer(
                _ =>
                {
                    try
                    {
                        if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsBossPlayerId))
                        {
                            var notification = notificationHelper.GenerateWsFriendsListAccept(mcsBotPlayerProfile, NotificationEventType.friendListRequestAccept);
                            notificationSendHelper.SendMessage(mcsBossPlayerId, notification);
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

        public async Task OnPostLoadAsync()
        {
            await LoadAllOrderInfos();
        }
    }
}