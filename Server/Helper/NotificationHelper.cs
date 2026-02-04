
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
            var data = mcsBotPlayerProfile.CharacterData.PmcData;
            return new WsGroupMatchInviteAccept
            {
                EventType = NotificationEventType.groupMatchInviteAccept,
                EventIdentifier = new MongoId(),
                Id = mcsBotPlayerProfile.ProfileInfo.ProfileId,
                Aid = mcsBotPlayerProfile.ProfileInfo.Aid,
                Info = new CharacterInfo
                {
                    Nickname = mcsBotPlayerProfile.CharacterData.ScavData.Info.MainProfileNickname,
                    Side = data.Info.Side,
                    Level = data.Info.Level,
                    MemberCategory = data.Info.MemberCategory,
                    GameVersion = data.Info.GameVersion,
                    SavageLockTime = data.Info.SavageLockTime,
                    SavageNickname = mcsBotPlayerProfile.CharacterData.ScavData.Info.Nickname,
                    HasCoopExtension = data.Info.HasCoopExtension
                },
                IsLeader = false,
                IsReady = false,
            };
        }

        public WsFriendsListAccept GenerateWsFriendsListAccept(SptProfile mcsBotPlayerProfile, NotificationEventType eventType)
        {
            var data = mcsBotPlayerProfile.CharacterData.PmcData;
            return new WsFriendsListAccept
            {
                EventType = eventType,
                Profile = new SearchFriendResponse()
                {
                    Id = mcsBotPlayerProfile.ProfileInfo.ProfileId.Value,
                    Aid = mcsBotPlayerProfile.ProfileInfo.Aid,
                    Info = new UserDialogDetails
                    {
                        Nickname = data.Info.Nickname,
                        Side = data.Info.Side,
                        Level = data.Info.Level,
                        MemberCategory = data.Info.MemberCategory,
                        SelectedMemberCategory = data.Info.SelectedMemberCategory
                    }
                }
            };
        }

        public WsGroupMatchRaidReady GenerateWsGroupMatchRaidReady(SptProfile mcsBotPlayerProfile, bool isScav)
        {
            var data = isScav ? mcsBotPlayerProfile.CharacterData.ScavData : mcsBotPlayerProfile.CharacterData.PmcData;
            return new WsGroupMatchRaidReady
            {
                EventType = NotificationEventType.groupMatchRaidReady,
                EventIdentifier = new MongoId(),
                ExtendedProfile = new GroupCharacter
                {
                    Id = data.Id.Value,
                    Aid = data.Aid,
                    Info = new CharacterInfo
                    {
                        Nickname = mcsBotPlayerProfile.CharacterData.ScavData.Info.MainProfileNickname,
                        Side = data.Info.Side,
                        Level = data.Info.Level,
                        MemberCategory = data.Info.MemberCategory,
                        GameVersion = data.Info.GameVersion,
                        SavageLockTime = data.Info.SavageLockTime,
                        SavageNickname = mcsBotPlayerProfile.CharacterData.ScavData.Info.Nickname,
                        HasCoopExtension = data.Info.HasCoopExtension,
                    },
                    VisualRepresentation = new PlayerVisualRepresentation
                    {
                        Info = new VisualInfo
                        {
                            Nickname = data.Info.Nickname,
                            Side = data.Info.Side,
                            MemberCategory = data.Info.MemberCategory,
                            Level = data.Info.Level,
                            GameVersion = data.Info.GameVersion,
                        },
                        Customization = new Customization
                        {
                            Head = data.Customization.Head,
                            Body = data.Customization.Body,
                            Feet = data.Customization.Feet,
                            Hands = data.Customization.Hands,
                        },
                        Equipment = new Equipment
                        {
                            Id = data.Inventory.Equipment,
                            Items = data.Inventory.Items,
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