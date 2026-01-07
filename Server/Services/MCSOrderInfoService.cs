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
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class MCSOrderInfoService(
        MCSConfigService mcsConfigService,
        MCSProfileService mcsProfileService,
        NotificationSendHelper notificationSendHelper,
        ISptLogger<MCSOrderInfoService> logger,
        SaveServer saveServer,
        MCSNotificationHelper mcsNotificationHelper,
        TimeUtil timeUtil,
        JsonUtil jsonUtil,
        FileUtil fileUtil
    )
    {
        private readonly string _orderFolderDir = System.IO.Path.Join(mcsConfigService.GetModPath(), "Assets", "database", "orders");
        private readonly ConcurrentDictionary<MongoId, MCSOrderInfo> _orderInfos = new();

        public void AddOrderInfo(MCSOrderInfo orderInfo)
        {
            _orderInfos.TryAdd(orderInfo.QuestId, orderInfo);
        }

        public void AddOrderInfos(List<MCSOrderInfo> orderInfos)
        {
            foreach (var orderInfo in orderInfos)
            {
                AddOrderInfo(orderInfo);
            }
        }

        public void RemoveOrderInfo(MCSOrderInfo orderInfo)
        {
            _orderInfos.TryRemove(orderInfo.QuestId, out _);
        }

        public void CreateOrderInfo(MongoId sessionId, int players, int carryServiceLevel, int duration, MongoId questId)
        {
            var hashSetPlayers = new HashSet<MongoId>();
            for (int i = 0; i < players; i++)
            {
                hashSetPlayers.Add(new MongoId());
            }
            var orderInfo = new MCSOrderInfo()
            {
                BossSessionId = sessionId,
                QuestId = questId,
                PlayerIds = hashSetPlayers,
                CarryServiceLevel = carryServiceLevel,
                Duration = duration,
                Status = EOrderInfoStatus.AvailableForStart,
                ExpirationTime = timeUtil.GetTimeStamp() + mcsConfigService.GetOrderConfig().OrderQuests.First().ResetTime
            };
            AddOrderInfo(orderInfo);
        }

        public void SaveOrderInfo()
        {
            var orderPath = System.IO.Path.Combine(_orderFolderDir, "orderinfo.json");
            var orderInfos = _orderInfos.Values.ToList();
            var jsonMCSOrderInfos = jsonUtil.Serialize(orderInfos, true);
            fileUtil.WriteFile(orderPath, jsonMCSOrderInfos);
        }

        public List<MCSOrderInfo> GetOrderInfos(MongoId sessionId)
        {
            List<MCSOrderInfo> targetMCSOrderInfos = new();

            foreach (var mcsOrderInfo in _orderInfos.Values.ToList())
            {
                if (mcsOrderInfo.BossSessionId == sessionId)
                {
                    targetMCSOrderInfos.Add(mcsOrderInfo);
                }
            }

            return targetMCSOrderInfos;
        }

        public List<MCSOrderInfo> GetAllOrderInfos()
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

            var orderInfos = await jsonUtil.DeserializeFromFileAsync<List<MCSOrderInfo>>(orderPath);
            foreach (var orderInfo in orderInfos)
            {
                _orderInfos[orderInfo.QuestId] = orderInfo;
            }
        }

        public void ProcessExpiredOrderInfos()
        {
            var orderInfos = GetAllOrderInfos();
            foreach (var orderInfo in orderInfos)
            {
                var currentTime = timeUtil.GetTimeStamp();
                if (currentTime >= orderInfo.ExpirationTime - 1)
                {
                    logger.Info($"准备清除 {orderInfo.BossSessionId} 的一个过期订单");
                    RemoveOrderInfo(orderInfo);
                    foreach (var csPlayerSessionId in orderInfo.PlayerIds)
                    {
                        logger.Info($"准备清除 {csPlayerSessionId} 的Profile");
                        mcsProfileService.ProcessExpiredCarryServiceProfile(orderInfo.BossSessionId, csPlayerSessionId);
                    }
                }
            }
            SaveOrderInfo();
        }

        public void SetOrderInfoStarted(MCSOrderInfo orderInfo, PmcData completeQuestPmcData)
        {
            if (orderInfo.Status == EOrderInfoStatus.AvailableForStart)
            {
                orderInfo.Status = EOrderInfoStatus.Started;
                var currentTime = timeUtil.GetTimeStamp();
                orderInfo.ExpirationTime = currentTime + orderInfo.Duration * 60; // 记得改回3600
                foreach (var csPlayerSessionId in orderInfo.PlayerIds)
                {
                    var csFullProfile = mcsProfileService.Generate(orderInfo.BossSessionId, csPlayerSessionId, completeQuestPmcData, orderInfo.CarryServiceLevel);
                    CompleteOrderQuestSendFriendRequest(csFullProfile, orderInfo.BossSessionId);
                }
            }
        }

        private void CompleteOrderQuestSendFriendRequest(SptProfile csFullProfile, MongoId sessionId)
        {
            var completeQuestPlayerFullProfile = saveServer.GetProfile(sessionId);
            completeQuestPlayerFullProfile?.FriendProfileIds?.Add(csFullProfile.ProfileInfo.ProfileId.Value);
            _ = new Timer(
                _ =>
                {
                    var notification = mcsNotificationHelper.GenerateWsFriendsListAccept(csFullProfile, NotificationEventType.friendListRequestAccept);
                    notificationSendHelper.SendMessage(sessionId, notification);
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