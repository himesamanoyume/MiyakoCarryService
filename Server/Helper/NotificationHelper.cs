
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Models.Eft.Match;
using MiyakoCarryService.Server.Models.Eft.Ws;
using MiyakoCarryService.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Services;

namespace MiyakoCarryService.Server.Helper
{
    [Injectable]
    public class NotificationHelper(
        ServerLocalisationService serverLocalisationService,
        ConfigController configController
    )
    {
        public WsGroupMatchInviteDecline GenerateWsGroupMatchInviteDecline(SptProfile mcsBotPlayerProfile)
        {
            return new WsGroupMatchInviteDecline
            {
                EventType = NotificationEventType.groupMatchInviteDecline,
                EventIdentifier = new(),
                Aid = mcsBotPlayerProfile.ProfileInfo.Aid,
                Nickname = mcsBotPlayerProfile.CharacterData.PmcData.Info.Nickname + $" [{configController.GetSpawnTypeDisplayName(mcsBotPlayerProfile.CharacterData.PmcData.Info.Settings.Role)}]"
            };
        }

        public WsGroupMatchInviteAccept GenerateWsGroupMatchInviteAccept(SptProfile mcsBotPlayerProfile)
        {
            var data = mcsBotPlayerProfile.CharacterData.PmcData;
            return new WsGroupMatchInviteAccept
            {
                EventType = NotificationEventType.groupMatchInviteAccept,
                EventIdentifier = new(),
                Id = mcsBotPlayerProfile.ProfileInfo.ProfileId,
                Aid = mcsBotPlayerProfile.ProfileInfo.Aid,
                Info = new CharacterInfo
                {
                    Nickname = mcsBotPlayerProfile.CharacterData.ScavData.Info.MainProfileNickname + $" [{configController.GetSpawnTypeDisplayName(data.Info.Settings.Role)}]",
                    Side = data.Info.Side,
                    Level = data.Info.Level,
                    MemberCategory = mcsBotPlayerProfile.CharacterData.PmcData.Info.MemberCategory,
                    GameVersion = data.Info.GameVersion,
                    SavageLockTime = data.Info.SavageLockTime,
                    SavageNickname = mcsBotPlayerProfile.CharacterData.ScavData.Info.Nickname,
                    HasCoopExtension = data.Info.HasCoopExtension
                },
                IsLeader = false,
                IsReady = false,
            };
        }

        public WsFriendsListAccept GenerateWsFriendsListAccept(SptProfile mcsBotPlayerProfile, NotificationEventType eventType, bool isExpired = false)
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
                        Nickname = isExpired ? $"({serverLocalisationService.GetText(Locales.MCSBOTPLAYEREXPIRED)}) {data.Info.Nickname}" : data.Info.Nickname + $" [{configController.GetSpawnTypeDisplayName(data.Info.Settings.Role)}]",
                        Side = data.Info.Side,
                        Level = data.Info.Level,
                        MemberCategory = data.Info.MemberCategory,
                        SelectedMemberCategory = data.Info.SelectedMemberCategory
                    }
                }
            };
        }

        public McsWsGroupMatchRaidReady GenerateWsGroupMatchRaidReady(SptProfile mcsBotPlayerProfile, bool isScav)
        {
            var data = isScav ? mcsBotPlayerProfile.CharacterData.ScavData : mcsBotPlayerProfile.CharacterData.PmcData;
            return new McsWsGroupMatchRaidReady
            {
                EventType = NotificationEventType.groupMatchRaidReady,
                EventIdentifier = new(),
                ExtendedProfile = new McsGroupCharacter
                {
                    Id = data.Id.Value,
                    Aid = data.Aid,
                    Info = new McsCharacterInfo
                    {
                        Nickname = mcsBotPlayerProfile.CharacterData.ScavData.Info.MainProfileNickname + $" [{configController.GetSpawnTypeDisplayName(data.Info.Settings.Role)}]",
                        Side = data.Info.Side,
                        Level = data.Info.Level,
                        MemberCategory = mcsBotPlayerProfile.CharacterData.PmcData.Info.MemberCategory,
                        GameVersion = data.Info.GameVersion,
                        SavageLockTime = data.Info.SavageLockTime,
                        SavageNickname = mcsBotPlayerProfile.CharacterData.ScavData.Info.Nickname,
                        HasCoopExtension = data.Info.HasCoopExtension,
                        Health = data.Health
                    },
                    VisualRepresentation = new McsPlayerVisualRepresentation
                    {
                        Info = new McsVisualInfo
                        {
                            Nickname = data.Info.Nickname,
                            Side = data.Info.Side,
                            MemberCategory = mcsBotPlayerProfile.CharacterData.PmcData.Info.MemberCategory,
                            Level = data.Info.Level,
                            GameVersion = data.Info.GameVersion,
                            Health = data.Health
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
                EventIdentifier = new(),
                Nickname = $"({serverLocalisationService.GetText(Locales.MCSBOTPLAYEREXPIRED)}) {mcsBotPlayerProfile.ProfileInfo.Username} [{configController.GetSpawnTypeDisplayName(mcsBotPlayerProfile.CharacterData.PmcData.Info.Settings.Role)}]",
                Aid = mcsBotPlayerProfile.ProfileInfo.Aid.Value,
            };
        }
    }
}