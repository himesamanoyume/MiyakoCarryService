
using MiyakoCarryService.Server.Models.Eft.Ws;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;

namespace MiyakoCarryService.Server.Helper
{
    [Injectable]
    public class NotificationHelper(

    )
    {
        public WsGroupMatchInviteDecline GenerateWsGroupMatchInviteDecline(SptProfile mcsPlayerFullProfile)
        {
            return new WsGroupMatchInviteDecline
            {
                EventType = NotificationEventType.groupMatchInviteDecline,
                EventIdentifier = new MongoId(),
                Aid = mcsPlayerFullProfile.ProfileInfo.Aid,
                Nickname = mcsPlayerFullProfile.CharacterData.PmcData.Info.Nickname
            };
        }

        public WsGroupMatchInviteAccept GenerateWsGroupMatchInviteAccept(SptProfile mcsPlayerFullProfile)
        {
            return new WsGroupMatchInviteAccept
            {
                EventType = NotificationEventType.groupMatchInviteAccept,
                EventIdentifier = new MongoId(),
                Id = mcsPlayerFullProfile.ProfileInfo.ProfileId,
                Aid = mcsPlayerFullProfile.ProfileInfo.Aid,
                Info = new CharacterInfo
                {
                    Nickname = mcsPlayerFullProfile.CharacterData.PmcData.Info.Nickname,
                    Side = mcsPlayerFullProfile.CharacterData.PmcData.Info.Side,
                    Level = mcsPlayerFullProfile.CharacterData.PmcData.Info.Level,
                    MemberCategory = mcsPlayerFullProfile.CharacterData.PmcData.Info.MemberCategory,
                    GameVersion = mcsPlayerFullProfile.CharacterData.PmcData.Info.GameVersion,
                    SavageLockTime = mcsPlayerFullProfile.CharacterData.ScavData.Info.SavageLockTime,
                    SavageNickname = mcsPlayerFullProfile.CharacterData.ScavData.Info.Nickname,
                    HasCoopExtension = mcsPlayerFullProfile.CharacterData.PmcData.Info.HasCoopExtension
                },
                VisualRepresentation = new PlayerVisualRepresentation
                {
                    Info = new VisualInfo
                    {
                        Nickname = mcsPlayerFullProfile.CharacterData.PmcData.Info.Nickname,
                        Side = mcsPlayerFullProfile.CharacterData.PmcData.Info.Side,
                        MemberCategory = mcsPlayerFullProfile.CharacterData.PmcData.Info.MemberCategory,
                        Level = mcsPlayerFullProfile.CharacterData.PmcData.Info.Level,
                        GameVersion = mcsPlayerFullProfile.CharacterData.PmcData.Info.GameVersion
                    },
                    Customization = new Customization
                    {
                        Head = mcsPlayerFullProfile.CharacterData.PmcData.Customization.Head,
                        Body = mcsPlayerFullProfile.CharacterData.PmcData.Customization.Body,
                        Feet = mcsPlayerFullProfile.CharacterData.PmcData.Customization.Feet,
                        Hands = mcsPlayerFullProfile.CharacterData.PmcData.Customization.Hands,
                    },
                    Equipment = new Equipment
                    {
                        Id = mcsPlayerFullProfile.CharacterData.PmcData.Inventory.Equipment,
                        Items = mcsPlayerFullProfile.CharacterData.PmcData.Inventory.Items
                    },
                },
                IsLeader = false,
                IsReady = false,
            };
        }

        public WsFriendsListAccept GenerateWsFriendsListAccept(SptProfile mcsPlayerFullProfile, NotificationEventType eventType)
        {
            return new WsFriendsListAccept
            {
                EventType = eventType,
                Profile = new SearchFriendResponse()
                {
                    Id = mcsPlayerFullProfile.ProfileInfo.ProfileId.Value,
                    Aid = mcsPlayerFullProfile.ProfileInfo.Aid,
                    Info = new UserDialogDetails
                    {
                        Nickname = mcsPlayerFullProfile.CharacterData.PmcData.Info.Nickname,
                        Side = mcsPlayerFullProfile.CharacterData.PmcData.Info.Side,
                        Level = mcsPlayerFullProfile.CharacterData.PmcData.Info.Level,
                        MemberCategory = mcsPlayerFullProfile.CharacterData.PmcData.Info.MemberCategory,
                        SelectedMemberCategory = mcsPlayerFullProfile.CharacterData.PmcData.Info.SelectedMemberCategory
                    }
                }
            };
        }

        public WsGroupMatchRaidReady GenerateWsGroupMatchRaidReady(SptProfile mcsPlayerFullProfile)
        {
            return new WsGroupMatchRaidReady
            {
                EventType = NotificationEventType.groupMatchRaidReady,
                EventIdentifier = new MongoId(),
                ExtendedProfile = new GroupCharacter
                {
                    Id = mcsPlayerFullProfile.CharacterData.PmcData.Id.Value,
                    Aid = mcsPlayerFullProfile.CharacterData.PmcData.Aid,
                    Info = new CharacterInfo
                    {
                        Nickname = mcsPlayerFullProfile.CharacterData.PmcData.Info.Nickname,
                        Side = mcsPlayerFullProfile.CharacterData.PmcData.Info.Side,
                        Level = mcsPlayerFullProfile.CharacterData.PmcData.Info.Level,
                        MemberCategory = mcsPlayerFullProfile.CharacterData.PmcData.Info.MemberCategory,
                        GameVersion = mcsPlayerFullProfile.CharacterData.PmcData.Info.GameVersion,
                        SavageLockTime = mcsPlayerFullProfile.CharacterData.ScavData.Info.SavageLockTime,
                        SavageNickname = mcsPlayerFullProfile.CharacterData.ScavData.Info.Nickname,
                        HasCoopExtension = mcsPlayerFullProfile.CharacterData.PmcData.Info.HasCoopExtension
                    },
                    VisualRepresentation = new PlayerVisualRepresentation
                    {
                        Info = new VisualInfo
                        {
                            Nickname = mcsPlayerFullProfile.CharacterData.PmcData.Info.Nickname,
                            Side = mcsPlayerFullProfile.CharacterData.PmcData.Info.Side,
                            MemberCategory = mcsPlayerFullProfile.CharacterData.PmcData.Info.MemberCategory,
                            Level = mcsPlayerFullProfile.CharacterData.PmcData.Info.Level,
                            GameVersion = mcsPlayerFullProfile.CharacterData.PmcData.Info.GameVersion
                        },
                        Customization = new Customization
                        {
                            Head = mcsPlayerFullProfile.CharacterData.PmcData.Customization.Head,
                            Body = mcsPlayerFullProfile.CharacterData.PmcData.Customization.Body,
                            Feet = mcsPlayerFullProfile.CharacterData.PmcData.Customization.Feet,
                            Hands = mcsPlayerFullProfile.CharacterData.PmcData.Customization.Hands,
                        },
                        Equipment = new Equipment
                        {
                            Id = mcsPlayerFullProfile.CharacterData.PmcData.Inventory.Equipment,
                            Items = mcsPlayerFullProfile.CharacterData.PmcData.Inventory.Items
                        },
                    },
                    IsLeader = false,
                    IsReady = true,
                    LookingGroup = true
                }
            };
        }

        public WsGroupMatchUserLeave GenerateWsGroupMatchUserLeave(SptProfile mcsPlayerFullProfile)
        {
            return new WsGroupMatchUserLeave
            {
                EventType = NotificationEventType.groupMatchUserLeave,
                EventIdentifier = new MongoId(),
                Nickname = mcsPlayerFullProfile.ProfileInfo.Username,
                Aid = mcsPlayerFullProfile.ProfileInfo.Aid.Value,
            };
        }
    }
}