
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
            if (orderInfo.Status == EOrderInfoStatus.AvailableForStart)
            {
                orderInfo.Status = EOrderInfoStatus.Started;
                var currentTime = timeUtil.GetTimeStamp();
                orderInfo.ExpirationTime = currentTime + orderInfo.Duration * 60; // 记得改回3600
                foreach (var csPlayerSessionId in orderInfo.PlayerIds)
                {
                    var csFullProfile = mcsProfileController.Generate(orderInfo.BossSessionId, csPlayerSessionId, completeQuestPmcData, orderInfo.CarryServiceLevel);
                    CompleteOrderQuestSendFriendRequest(csFullProfile, orderInfo.BossSessionId);
                }
            }
        }

        public List<MCSOrderInfo> GetOrderInfos(MongoId sessionId)
        {
            return mcsOrderInfoService.GetOrderInfos(sessionId);
        }

        private void CompleteOrderQuestSendFriendRequest(SptProfile csFullProfile, MongoId sessionId)
        {
            var completeQuestPlayerFullProfile = saveServer.GetProfile(sessionId);
            completeQuestPlayerFullProfile?.FriendProfileIds?.Add(csFullProfile.ProfileInfo.ProfileId.Value);
            _ = new Timer(
                _ =>
                {
                    var notification = new WsFriendsListAccept
                    {
                        EventType = NotificationEventType.friendListRequestAccept,
                        Profile = new SearchFriendResponse()
                        {
                            Id = csFullProfile.ProfileInfo.ProfileId.Value,
                            Aid = csFullProfile.ProfileInfo.Aid,
                            Info = new UserDialogDetails
                            {
                                Nickname = csFullProfile.CharacterData.PmcData.Info.Nickname,
                                Side = csFullProfile.CharacterData.PmcData.Info.Side,
                                Level = csFullProfile.CharacterData.PmcData.Info.Level,
                                MemberCategory = csFullProfile.CharacterData.PmcData.Info.MemberCategory,
                                SelectedMemberCategory = csFullProfile.CharacterData.PmcData.Info.SelectedMemberCategory
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