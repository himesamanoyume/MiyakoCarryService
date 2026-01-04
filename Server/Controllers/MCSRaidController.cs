
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSRaidController(
        MCSRaidService mcsRaidService,
        NotificationSendHelper notificationSendHelper,
        MCSProfileController mcsProfileController
    )
    {
        public void AddGroupMember(MongoId bossSessionId, int csAid)
        {
            mcsRaidService.AddGroupMember(bossSessionId, csAid);
        }

        public void RemoveGroupMember(MongoId bossSessionId, int csAid)
        {
            mcsRaidService.RemoveGroupMember(bossSessionId, csAid);
        }

        public bool CheckCSPlayerExist(MongoId bossSessionId, int csAid)
        {
            return mcsRaidService.CheckCSPlayerExist(bossSessionId, csAid);
        }

        public void AcceptGroupInvite(MongoId bossSessionId, int csAid)
        {
            var csFullProfile = mcsProfileController.GetCSFullProfileByAccountId(bossSessionId, csAid);

            if (csFullProfile is null)
            {
                return;
            }

            if (CheckCSPlayerExist(bossSessionId, csAid))
            {
                var notification = new WsGroupMatchInviteDecline
                {
                    EventType = NotificationEventType.groupMatchInviteDecline,
                    EventIdentifier = new MongoId(),
                    Aid = csAid,
                    Nickname = csFullProfile.CharacterData.PmcData.Info.Nickname
                };
                notificationSendHelper.SendMessage(bossSessionId, notification);
            }
            else
            {
                var csPmcData = csFullProfile.CharacterData.PmcData;
                var csScavData = csFullProfile.CharacterData.ScavData;
                
                var characterInfo = new CharacterInfo
                {
                    Nickname = csPmcData.Info.Nickname,
                    Side = csPmcData.Info.Side,
                    Level = csPmcData.Info.Level,
                    MemberCategory = csPmcData.Info.MemberCategory,
                    GameVersion = csPmcData.Info.GameVersion,
                    SavageLockTime = csScavData.Info.SavageLockTime,
                    SavageNickname = csScavData.Info.Nickname,
                    HasCoopExtension = csPmcData.Info.HasCoopExtension
                };

                var visualRepresentation = new PlayerVisualRepresentation
                {
                    Info = new VisualInfo
                    {
                        Nickname = csPmcData.Info.Nickname,
                        Side = csPmcData.Info.Side,
                        MemberCategory = csPmcData.Info.MemberCategory,
                        Level = csPmcData.Info.Level,
                        GameVersion = csPmcData.Info.GameVersion
                    },
                    Customization = new Customization
                    {
                        Head = csPmcData.Customization.Head,
                        Body = csPmcData.Customization.Body,
                        Feet = csPmcData.Customization.Feet,
                        Hands = csPmcData.Customization.Hands,
                    },
                    Equipment = new Equipment
                    {
                        Id = csPmcData.Inventory.Equipment,
                        Items = csPmcData.Inventory.Items
                    },
                };

                var notification = new WsGroupMatchInviteAccept
                {
                    EventType = NotificationEventType.groupMatchInviteAccept,
                    EventIdentifier = new MongoId(),
                    Id = csFullProfile.ProfileInfo.ProfileId,
                    Aid = csAid,
                    Info = characterInfo,
                    VisualRepresentation = visualRepresentation,
                    IsLeader = false,
                    IsReady = false,
                };
                notificationSendHelper.SendMessage(bossSessionId, notification);

                var notification2 = new WsGroupMatchRaidReady
                {
                    EventType = NotificationEventType.groupMatchRaidReady,
                    EventIdentifier = new MongoId(),
                    ExtendedProfile = new GroupCharacter
                    {
                        Id = csPmcData.Id.Value,
                        Aid = csPmcData.Aid,
                        Info = characterInfo,
                        VisualRepresentation = visualRepresentation,
                        IsLeader = false,
                        IsReady = true,
                        LookingGroup = true
                    }
                };
                notificationSendHelper.SendMessage(bossSessionId, notification2);
            }

        }
    }
}