
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
        public WsGroupMatchInviteDecline GenerateWsGroupMatchInviteDecline(SptProfile mcsBotPlayerFullProfile)
        {
            return new WsGroupMatchInviteDecline
            {
                EventType = NotificationEventType.groupMatchInviteDecline,
                EventIdentifier = new MongoId(),
                Aid = mcsBotPlayerFullProfile.ProfileInfo.Aid,
                Nickname = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Nickname
            };
        }

        public WsGroupMatchInviteAccept GenerateWsGroupMatchInviteAccept(SptProfile mcsBotPlayerFullProfile)
        {
            return new WsGroupMatchInviteAccept
            {
                EventType = NotificationEventType.groupMatchInviteAccept,
                EventIdentifier = new MongoId(),
                Id = mcsBotPlayerFullProfile.ProfileInfo.ProfileId,
                Aid = mcsBotPlayerFullProfile.ProfileInfo.Aid,
                Info = new CharacterInfo
                {
                    Nickname = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Nickname,
                    Side = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Side,
                    Level = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Level,
                    MemberCategory = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.MemberCategory,
                    GameVersion = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.GameVersion,
                    SavageLockTime = mcsBotPlayerFullProfile.CharacterData.ScavData.Info.SavageLockTime,
                    SavageNickname = mcsBotPlayerFullProfile.CharacterData.ScavData.Info.Nickname,
                    HasCoopExtension = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.HasCoopExtension
                },
                VisualRepresentation = new PlayerVisualRepresentation
                {
                    Info = new VisualInfo
                    {
                        Nickname = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Nickname,
                        Side = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Side,
                        MemberCategory = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.MemberCategory,
                        Level = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Level,
                        GameVersion = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.GameVersion
                    },
                    Customization = new Customization
                    {
                        Head = mcsBotPlayerFullProfile.CharacterData.PmcData.Customization.Head,
                        Body = mcsBotPlayerFullProfile.CharacterData.PmcData.Customization.Body,
                        Feet = mcsBotPlayerFullProfile.CharacterData.PmcData.Customization.Feet,
                        Hands = mcsBotPlayerFullProfile.CharacterData.PmcData.Customization.Hands,
                    },
                    Equipment = new Equipment
                    {
                        Id = mcsBotPlayerFullProfile.CharacterData.PmcData.Inventory.Equipment,
                        Items = mcsBotPlayerFullProfile.CharacterData.PmcData.Inventory.Items
                    },
                },
                IsLeader = false,
                IsReady = false,
            };
        }

        public WsFriendsListAccept GenerateWsFriendsListAccept(SptProfile mcsBotPlayerFullProfile, NotificationEventType eventType)
        {
            return new WsFriendsListAccept
            {
                EventType = eventType,
                Profile = new SearchFriendResponse()
                {
                    Id = mcsBotPlayerFullProfile.ProfileInfo.ProfileId.Value,
                    Aid = mcsBotPlayerFullProfile.ProfileInfo.Aid,
                    Info = new UserDialogDetails
                    {
                        Nickname = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Nickname,
                        Side = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Side,
                        Level = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Level,
                        MemberCategory = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.MemberCategory,
                        SelectedMemberCategory = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.SelectedMemberCategory
                    }
                }
            };
        }

        public WsGroupMatchRaidReady GenerateWsGroupMatchRaidReady(SptProfile mcsBotPlayerFullProfile)
        {
            return new WsGroupMatchRaidReady
            {
                EventType = NotificationEventType.groupMatchRaidReady,
                EventIdentifier = new MongoId(),
                ExtendedProfile = new GroupCharacter
                {
                    Id = mcsBotPlayerFullProfile.CharacterData.PmcData.Id.Value,
                    Aid = mcsBotPlayerFullProfile.CharacterData.PmcData.Aid,
                    Info = new CharacterInfo
                    {
                        Nickname = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Nickname,
                        Side = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Side,
                        Level = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Level,
                        MemberCategory = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.MemberCategory,
                        GameVersion = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.GameVersion,
                        SavageLockTime = mcsBotPlayerFullProfile.CharacterData.ScavData.Info.SavageLockTime,
                        SavageNickname = mcsBotPlayerFullProfile.CharacterData.ScavData.Info.Nickname,
                        HasCoopExtension = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.HasCoopExtension
                    },
                    VisualRepresentation = new PlayerVisualRepresentation
                    {
                        Info = new VisualInfo
                        {
                            Nickname = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Nickname,
                            Side = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Side,
                            MemberCategory = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.MemberCategory,
                            Level = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Level,
                            GameVersion = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.GameVersion
                        },
                        Customization = new Customization
                        {
                            Head = mcsBotPlayerFullProfile.CharacterData.PmcData.Customization.Head,
                            Body = mcsBotPlayerFullProfile.CharacterData.PmcData.Customization.Body,
                            Feet = mcsBotPlayerFullProfile.CharacterData.PmcData.Customization.Feet,
                            Hands = mcsBotPlayerFullProfile.CharacterData.PmcData.Customization.Hands,
                        },
                        Equipment = new Equipment
                        {
                            Id = mcsBotPlayerFullProfile.CharacterData.PmcData.Inventory.Equipment,
                            Items = mcsBotPlayerFullProfile.CharacterData.PmcData.Inventory.Items
                        },
                    },
                    IsLeader = false,
                    IsReady = true,
                    LookingGroup = true
                }
            };
        }

        public WsGroupMatchUserLeave GenerateWsGroupMatchUserLeave(SptProfile mcsBotPlayerFullProfile)
        {
            return new WsGroupMatchUserLeave
            {
                EventType = NotificationEventType.groupMatchUserLeave,
                EventIdentifier = new MongoId(),
                Nickname = mcsBotPlayerFullProfile.ProfileInfo.Username,
                Aid = mcsBotPlayerFullProfile.ProfileInfo.Aid.Value,
            };
        }
    }
}