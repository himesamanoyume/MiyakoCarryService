
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;

namespace MiyakoCarryService.Server.Helper
{
    [Injectable]
    public class MCSNotificationHelper(

    )
    {
        public WsGroupMatchInviteDecline GenerateWsGroupMatchInviteDecline(SptProfile csFullProfile)
        {
            return new WsGroupMatchInviteDecline
            {
                EventType = NotificationEventType.groupMatchInviteDecline,
                EventIdentifier = new MongoId(),
                Aid = csFullProfile.ProfileInfo.Aid,
                Nickname = csFullProfile.CharacterData.PmcData.Info.Nickname
            };
        }

        public WsGroupMatchInviteAccept GenerateWsGroupMatchInviteAccept(SptProfile csFullProfile)
        {
            return new WsGroupMatchInviteAccept
            {
                EventType = NotificationEventType.groupMatchInviteAccept,
                EventIdentifier = new MongoId(),
                Id = csFullProfile.ProfileInfo.ProfileId,
                Aid = csFullProfile.ProfileInfo.Aid,
                Info = new CharacterInfo
                {
                    Nickname = csFullProfile.CharacterData.PmcData.Info.Nickname,
                    Side = csFullProfile.CharacterData.PmcData.Info.Side,
                    Level = csFullProfile.CharacterData.PmcData.Info.Level,
                    MemberCategory = csFullProfile.CharacterData.PmcData.Info.MemberCategory,
                    GameVersion = csFullProfile.CharacterData.PmcData.Info.GameVersion,
                    SavageLockTime = csFullProfile.CharacterData.ScavData.Info.SavageLockTime,
                    SavageNickname = csFullProfile.CharacterData.ScavData.Info.Nickname,
                    HasCoopExtension = csFullProfile.CharacterData.PmcData.Info.HasCoopExtension
                },
                VisualRepresentation = new PlayerVisualRepresentation
                {
                    Info = new VisualInfo
                    {
                        Nickname = csFullProfile.CharacterData.PmcData.Info.Nickname,
                        Side = csFullProfile.CharacterData.PmcData.Info.Side,
                        MemberCategory = csFullProfile.CharacterData.PmcData.Info.MemberCategory,
                        Level = csFullProfile.CharacterData.PmcData.Info.Level,
                        GameVersion = csFullProfile.CharacterData.PmcData.Info.GameVersion
                    },
                    Customization = new Customization
                    {
                        Head = csFullProfile.CharacterData.PmcData.Customization.Head,
                        Body = csFullProfile.CharacterData.PmcData.Customization.Body,
                        Feet = csFullProfile.CharacterData.PmcData.Customization.Feet,
                        Hands = csFullProfile.CharacterData.PmcData.Customization.Hands,
                    },
                    Equipment = new Equipment
                    {
                        Id = csFullProfile.CharacterData.PmcData.Inventory.Equipment,
                        Items = csFullProfile.CharacterData.PmcData.Inventory.Items
                    },
                },
                IsLeader = false,
                IsReady = false,
            };
        }

        public WsFriendsListAccept GenerateWsFriendsListAccept(SptProfile csFullProfile, NotificationEventType eventType)
        {
            return new WsFriendsListAccept
            {
                EventType = eventType,
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
        }

        public WsGroupMatchRaidReady GenerateWsGroupMatchRaidReady(SptProfile csFullProfile)
        {
            return new WsGroupMatchRaidReady
            {
                EventType = NotificationEventType.groupMatchRaidReady,
                EventIdentifier = new MongoId(),
                ExtendedProfile = new GroupCharacter
                {
                    Id = csFullProfile.CharacterData.PmcData.Id.Value,
                    Aid = csFullProfile.CharacterData.PmcData.Aid,
                    Info = new CharacterInfo
                    {
                        Nickname = csFullProfile.CharacterData.PmcData.Info.Nickname,
                        Side = csFullProfile.CharacterData.PmcData.Info.Side,
                        Level = csFullProfile.CharacterData.PmcData.Info.Level,
                        MemberCategory = csFullProfile.CharacterData.PmcData.Info.MemberCategory,
                        GameVersion = csFullProfile.CharacterData.PmcData.Info.GameVersion,
                        SavageLockTime = csFullProfile.CharacterData.ScavData.Info.SavageLockTime,
                        SavageNickname = csFullProfile.CharacterData.ScavData.Info.Nickname,
                        HasCoopExtension = csFullProfile.CharacterData.PmcData.Info.HasCoopExtension
                    },
                    VisualRepresentation = new PlayerVisualRepresentation
                    {
                        Info = new VisualInfo
                        {
                            Nickname = csFullProfile.CharacterData.PmcData.Info.Nickname,
                            Side = csFullProfile.CharacterData.PmcData.Info.Side,
                            MemberCategory = csFullProfile.CharacterData.PmcData.Info.MemberCategory,
                            Level = csFullProfile.CharacterData.PmcData.Info.Level,
                            GameVersion = csFullProfile.CharacterData.PmcData.Info.GameVersion
                        },
                        Customization = new Customization
                        {
                            Head = csFullProfile.CharacterData.PmcData.Customization.Head,
                            Body = csFullProfile.CharacterData.PmcData.Customization.Body,
                            Feet = csFullProfile.CharacterData.PmcData.Customization.Feet,
                            Hands = csFullProfile.CharacterData.PmcData.Customization.Hands,
                        },
                        Equipment = new Equipment
                        {
                            Id = csFullProfile.CharacterData.PmcData.Inventory.Equipment,
                            Items = csFullProfile.CharacterData.PmcData.Inventory.Items
                        },
                    },
                    IsLeader = false,
                    IsReady = true,
                    LookingGroup = true
                }
            };
        }
    }
}