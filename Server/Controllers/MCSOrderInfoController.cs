
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Models.Enums;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSOrderInfoController(
        MCSOrderInfoService mcsOrderInfoService,
        MCSProfileController mcsProfileController,
        MCSConfigController mcsConfigController,
        NotificationSendHelper notificationSendHelper,
        ProfileHelper profileHelper,
        SaveServer saveServer,
        TimeUtil timeUtil
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
                ExpirationTime = timeUtil.GetTimeStamp() + mcsConfigController.GetOrderConfig().OrderQuests.First().ResetTime
            };
            mcsOrderInfoService.AddOrderInfo(orderInfo);
        }

        public void ProcessExpiredOrderInfos()
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

        public void SetOrderInfoStarted(MCSOrderInfo orderInfo, PmcData pmcData)
        {
            if (orderInfo.Status == EOrderInfoStatus.AvailableForStart)
            {
                orderInfo.Status = EOrderInfoStatus.Started;
                var currentTime = timeUtil.GetTimeStamp();
                orderInfo.ExpirationTime = currentTime + orderInfo.Duration * 3600;
                var csBotBases = new List<BotBase>();
                foreach (var csPlayerSessionId in orderInfo.PlayerIds)
                {
                    var botBase = mcsProfileController.GenerateBotProfile(orderInfo.SessionId, pmcData, orderInfo.CarryServiceLevel);
                    botBase.SessionId = csPlayerSessionId;
                    csBotBases.Add(botBase);
                    mcsProfileController.SaveMCPlayerProfile(orderInfo.SessionId, botBase);

                    CompleteOrderQuestSendFriendRequest(pmcData, botBase, orderInfo.SessionId);
                }
            }
        }

        public List<MCSOrderInfo> GetOrderInfos(MongoId sessionId)
        {
            return mcsOrderInfoService.GetOrderInfos(sessionId);
        }

        public void CompleteOrderQuestSendFriendRequest(PmcData pmcData, BotBase botBase, MongoId sessionId)
        {
            var profile = saveServer.GetProfile(sessionId);
            profile.FriendProfileIds.Add((MongoId)botBase.SessionId);
            _ = new Timer(
                _ =>
                {
                    var notification = new WsFriendsListAccept
                    {
                        EventType = NotificationEventType.friendListRequestAccept,
                        Profile = profileHelper.GetChatRoomMemberFromPmcProfile(pmcData),
                    };
                    notificationSendHelper.SendMessage(sessionId, notification);
                },
                null,
                TimeSpan.FromMicroseconds(1000),
                Timeout.InfiniteTimeSpan
            );
        }
    }
}