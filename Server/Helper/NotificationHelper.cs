
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
        public WsGroupMatchInviteDecline GenerateWsGroupMatchInviteDecline(SptProfile mcsBotPlayerProfile)
        {
            return new WsGroupMatchInviteDecline
            {
                EventType = NotificationEventType.groupMatchInviteDecline,
                EventIdentifier = new MongoId(),
                Aid = mcsBotPlayerProfile.ProfileInfo.Aid,
                Nickname = mcsBotPlayerProfile.CharacterData.PmcData.Info.Nickname
            };
        }

        public WsGroupMatchInviteAccept GenerateWsGroupMatchInviteAccept(SptProfile mcsBotPlayerProfile)
        {
            return new WsGroupMatchInviteAccept
            {
                EventType = NotificationEventType.groupMatchInviteAccept,
                EventIdentifier = new MongoId(),
                Id = mcsBotPlayerProfile.ProfileInfo.ProfileId,
                Aid = mcsBotPlayerProfile.ProfileInfo.Aid,
                Info = new CharacterInfo
                {
                    Nickname = mcsBotPlayerProfile.CharacterData.PmcData.Info.Nickname,
                    Side = mcsBotPlayerProfile.CharacterData.PmcData.Info.Side,
                    Level = mcsBotPlayerProfile.CharacterData.PmcData.Info.Level,
                    MemberCategory = mcsBotPlayerProfile.CharacterData.PmcData.Info.MemberCategory,
                    GameVersion = mcsBotPlayerProfile.CharacterData.PmcData.Info.GameVersion,
                    SavageLockTime = mcsBotPlayerProfile.CharacterData.ScavData.Info.SavageLockTime,
                    SavageNickname = mcsBotPlayerProfile.CharacterData.ScavData.Info.Nickname,
                    HasCoopExtension = mcsBotPlayerProfile.CharacterData.PmcData.Info.HasCoopExtension
                },
                VisualRepresentation = new PlayerVisualRepresentation
                {
                    Info = new VisualInfo
                    {
                        Nickname = mcsBotPlayerProfile.CharacterData.PmcData.Info.Nickname,
                        Side = mcsBotPlayerProfile.CharacterData.PmcData.Info.Side,
                        MemberCategory = mcsBotPlayerProfile.CharacterData.PmcData.Info.MemberCategory,
                        Level = mcsBotPlayerProfile.CharacterData.PmcData.Info.Level,
                        GameVersion = mcsBotPlayerProfile.CharacterData.PmcData.Info.GameVersion
                    },
                    Customization = new Customization
                    {
                        Head = mcsBotPlayerProfile.CharacterData.PmcData.Customization.Head,
                        Body = mcsBotPlayerProfile.CharacterData.PmcData.Customization.Body,
                        Feet = mcsBotPlayerProfile.CharacterData.PmcData.Customization.Feet,
                        Hands = mcsBotPlayerProfile.CharacterData.PmcData.Customization.Hands,
                    },
                    Equipment = new Equipment
                    {
                        Id = mcsBotPlayerProfile.CharacterData.PmcData.Inventory.Equipment,
                        Items = mcsBotPlayerProfile.CharacterData.PmcData.Inventory.Items
                    },
                },
                IsLeader = false,
                IsReady = false,
            };
        }

        public WsFriendsListAccept GenerateWsFriendsListAccept(SptProfile mcsBotPlayerProfile, NotificationEventType eventType)
        {
            return new WsFriendsListAccept
            {
                EventType = eventType,
                Profile = new SearchFriendResponse()
                {
                    Id = mcsBotPlayerProfile.ProfileInfo.ProfileId.Value,
                    Aid = mcsBotPlayerProfile.ProfileInfo.Aid,
                    Info = new UserDialogDetails
                    {
                        Nickname = mcsBotPlayerProfile.CharacterData.PmcData.Info.Nickname,
                        Side = mcsBotPlayerProfile.CharacterData.PmcData.Info.Side,
                        Level = mcsBotPlayerProfile.CharacterData.PmcData.Info.Level,
                        MemberCategory = mcsBotPlayerProfile.CharacterData.PmcData.Info.MemberCategory,
                        SelectedMemberCategory = mcsBotPlayerProfile.CharacterData.PmcData.Info.SelectedMemberCategory
                    }
                }
            };
        }

        public WsGroupMatchRaidReady GenerateWsGroupMatchRaidReady(SptProfile mcsBotPlayerProfile)
        {
            return new WsGroupMatchRaidReady
            {
                EventType = NotificationEventType.groupMatchRaidReady,
                EventIdentifier = new MongoId(),
                ExtendedProfile = new GroupCharacter
                {
                    Id = mcsBotPlayerProfile.CharacterData.PmcData.Id.Value,
                    Aid = mcsBotPlayerProfile.CharacterData.PmcData.Aid,
                    Info = new CharacterInfo
                    {
                        Nickname = mcsBotPlayerProfile.CharacterData.PmcData.Info.Nickname,
                        Side = mcsBotPlayerProfile.CharacterData.PmcData.Info.Side,
                        Level = mcsBotPlayerProfile.CharacterData.PmcData.Info.Level,
                        MemberCategory = mcsBotPlayerProfile.CharacterData.PmcData.Info.MemberCategory,
                        GameVersion = mcsBotPlayerProfile.CharacterData.PmcData.Info.GameVersion,
                        SavageLockTime = mcsBotPlayerProfile.CharacterData.ScavData.Info.SavageLockTime,
                        SavageNickname = mcsBotPlayerProfile.CharacterData.ScavData.Info.Nickname,
                        HasCoopExtension = mcsBotPlayerProfile.CharacterData.PmcData.Info.HasCoopExtension
                    },
                    VisualRepresentation = new PlayerVisualRepresentation
                    {
                        Info = new VisualInfo
                        {
                            Nickname = mcsBotPlayerProfile.CharacterData.PmcData.Info.Nickname,
                            Side = mcsBotPlayerProfile.CharacterData.PmcData.Info.Side,
                            MemberCategory = mcsBotPlayerProfile.CharacterData.PmcData.Info.MemberCategory,
                            Level = mcsBotPlayerProfile.CharacterData.PmcData.Info.Level,
                            GameVersion = mcsBotPlayerProfile.CharacterData.PmcData.Info.GameVersion
                        },
                        Customization = new Customization
                        {
                            Head = mcsBotPlayerProfile.CharacterData.PmcData.Customization.Head,
                            Body = mcsBotPlayerProfile.CharacterData.PmcData.Customization.Body,
                            Feet = mcsBotPlayerProfile.CharacterData.PmcData.Customization.Feet,
                            Hands = mcsBotPlayerProfile.CharacterData.PmcData.Customization.Hands,
                        },
                        Equipment = new Equipment
                        {
                            Id = mcsBotPlayerProfile.CharacterData.PmcData.Inventory.Equipment,
                            Items = mcsBotPlayerProfile.CharacterData.PmcData.Inventory.Items
                        },
                    },
                    IsLeader = false,
                    IsReady = true,
                    LookingGroup = true
                }
            };
        }

        public WsGroupMatchUserLeave GenerateWsGroupMatchUserLeave(SptProfile mcsBotPlayerProfile)
        {
            return new WsGroupMatchUserLeave
            {
                EventType = NotificationEventType.groupMatchUserLeave,
                EventIdentifier = new MongoId(),
                Nickname = mcsBotPlayerProfile.ProfileInfo.Username,
                Aid = mcsBotPlayerProfile.ProfileInfo.Aid.Value,
            };
        }
    }
}