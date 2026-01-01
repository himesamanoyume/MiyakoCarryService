
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
using SPTarkov.Server.Core.Models.Eft.Profile;
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
        SaveServer saveServer,
        HashUtil hashUtil,
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
                BossSessionId = sessionId,
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
                        mcsProfileController.ProcessExpiredCarryServiceProfile(orderInfo.BossSessionId, csPlayerSessionId);
                    }
                }
            }
            mcsOrderInfoService.SaveOrderInfo();
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
                    Console.WriteLine("生成护航存档");
                    var csBotBase = mcsProfileController.GenerateBotProfile(orderInfo.BossSessionId, completeQuestPmcData, orderInfo.CarryServiceLevel);
                    csBotBase.Id = csPlayerSessionId;
                    csBotBase.SessionId = csPlayerSessionId;
                    csBotBase.Aid = hashUtil.GenerateAccountId();
                    Console.WriteLine(csBotBase.SessionId);
                    orderInfo.PlayerIds.Add((MongoId)csBotBase.SessionId);
                    mcsProfileController.SaveMCPlayerProfile(orderInfo.BossSessionId, csBotBase);
                    CompleteOrderQuestSendFriendRequest(csBotBase, orderInfo.BossSessionId);
                }
            }
        }

        public List<MCSOrderInfo> GetOrderInfos(MongoId sessionId)
        {
            return mcsOrderInfoService.GetOrderInfos(sessionId);
        }

        public void CompleteOrderQuestSendFriendRequest(BotBase csBotBase, MongoId sessionId)
        {
            var completeQuestProfile = saveServer.GetProfile(sessionId);
            completeQuestProfile.FriendProfileIds.Add((MongoId)csBotBase.SessionId);
            _ = new Timer(
                _ =>
                {
                    var notification = new WsFriendsListAccept
                    {
                        EventType = NotificationEventType.friendListRequestAccept,
                        Profile = new SearchFriendResponse()
                        {
                            Id = csBotBase.Id.Value,
                            Aid = csBotBase.Aid,
                            Info = new UserDialogDetails
                            {
                                Nickname = csBotBase.Info.Nickname,
                                Side = csBotBase.Info.Side,
                                Level = csBotBase.Info.Level,
                                MemberCategory = csBotBase.Info.MemberCategory,
                                SelectedMemberCategory = csBotBase.Info.SelectedMemberCategory
                            }
                        }
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