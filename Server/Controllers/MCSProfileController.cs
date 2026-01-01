

using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Servers;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSProfileController(
        MCSProfileService mcsProfileService,
        NotificationSendHelper notificationSendHelper,
        SaveServer saveServer
    )
    {
        public void ProcessExpiredCarryServiceProfile(MongoId sessionId, MongoId csPlayerSessionId)
        {
            var completeQuestProfile = saveServer.GetProfile(sessionId);
            completeQuestProfile?.FriendProfileIds?.Remove(csPlayerSessionId);
            var csBotBase = mcsProfileService.GetBotBase(sessionId, csPlayerSessionId);
            var notification = new WsFriendsListAccept
            {
                EventType = NotificationEventType.youAreRemovedFromFriendList,
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
            mcsProfileService.RemoveProfile(sessionId, csPlayerSessionId);
        }

        public BotBase GenerateBotProfile(MongoId sessionId, PmcData pmcData, int carryServiceLevel)
        {
            return mcsProfileService.GenerateBotProfile(sessionId, pmcData, carryServiceLevel);
        }

        public void SaveMCPlayerProfile(MongoId sessionId, BotBase csProfile)
        {
            mcsProfileService.SaveMCPlayerProfile(sessionId, csProfile);
        }
    }
}