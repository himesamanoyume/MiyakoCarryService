

using System;
using System.Threading;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
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
    public class MCSProfileController(
        MCSProfileService mcsProfileService,
        NotificationSendHelper notificationSendHelper,
        PlayerScavGenerator playerScavGenerator,
        HashUtil hashUtil,
        SaveServer saveServer
    )
    {
        public void ProcessExpiredCarryServiceProfile(MongoId sessionId, MongoId csPlayerSessionId)
        {
            var completeQuestPlayerFullProfile = saveServer.GetProfile(sessionId);
            completeQuestPlayerFullProfile?.FriendProfileIds?.Remove(csPlayerSessionId);
            var csFullProfile = mcsProfileService.GetCSFullProfile(sessionId, csPlayerSessionId);
            _ = new Timer(
                _ =>
                {
                    var notification = new WsFriendsListAccept
                    {
                        EventType = NotificationEventType.youAreRemovedFromFriendList,
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
                    mcsProfileService.RemoveProfile(sessionId, csPlayerSessionId);
                },
                null,
                TimeSpan.FromMicroseconds(1000),
                Timeout.InfiniteTimeSpan
            );
        }

        protected BotBase GeneratePmcBotBaseProfile(MongoId sessionId, PmcData pmcData, int carryServiceLevel)
        {
            return mcsProfileService.GeneratePmcBotProfile(sessionId, pmcData, carryServiceLevel);
        }

        public void SaveCSPlayerProfile(MongoId sessionId, SptProfile csProfile)
        {
            mcsProfileService.SaveCSPlayerProfile(sessionId, csProfile);
        }

        public SptProfile Generate(MongoId bossSessionId, MongoId csPlayerSessionId, PmcData completeQuestPmcData, int carryServiceLevel)
        {
            var csPmcBotBase = GeneratePmcBotBaseProfile(bossSessionId, completeQuestPmcData, carryServiceLevel);
            csPmcBotBase.Id = csPlayerSessionId;
            csPmcBotBase.SessionId = csPlayerSessionId;
            csPmcBotBase.Aid = hashUtil.GenerateAccountId();

            var csFullProfile = GenerateCSFullProfile(csPmcBotBase);
            var csScavBotBase = playerScavGenerator.Generate(bossSessionId);

            csScavBotBase.Id = new MongoId();
            csScavBotBase.SessionId = csPmcBotBase.SessionId;
            csScavBotBase.Aid = csPmcBotBase.Aid;
            csScavBotBase.Info.MainProfileNickname = csPmcBotBase.Info.Nickname;
            csFullProfile.ProfileInfo.Aid = csPmcBotBase.Aid;
            csFullProfile.ProfileInfo.ScavengerId = csScavBotBase.Id;
            csFullProfile.CharacterData.PmcData.Savage = csScavBotBase.Id;
            csFullProfile.CharacterData.ScavData = csScavBotBase;

            SaveCSPlayerProfile(bossSessionId, csFullProfile);
            return csFullProfile;
        }

        protected SptProfile GenerateCSFullProfile(BotBase csPmcBotBase)
        {
            return new SptProfile
            {
                ProfileInfo = new SPTarkov.Server.Core.Models.Eft.Profile.Info
                {
                    ProfileId = csPmcBotBase.SessionId,
                    Username = csPmcBotBase.Info.Nickname
                },
                CharacterData = new Characters
                {
                    PmcData = new PmcData
                    {
                        Id = csPmcBotBase.Id,
                        Aid = csPmcBotBase.Aid,
                        SessionId = csPmcBotBase.SessionId,
                        KarmaValue = csPmcBotBase.KarmaValue,
                        Info = csPmcBotBase.Info,
                        Customization = csPmcBotBase.Customization,
                        Health = csPmcBotBase.Health,
                        Inventory = csPmcBotBase.Inventory,
                        Skills = csPmcBotBase.Skills,
                        Stats = csPmcBotBase.Stats,
                        Encyclopedia = csPmcBotBase.Encyclopedia,
                        TaskConditionCounters = csPmcBotBase.TaskConditionCounters,
                        InsuredItems = csPmcBotBase.InsuredItems,
                        Hideout = csPmcBotBase.Hideout,
                        Quests = csPmcBotBase.Quests,
                        TradersInfo = csPmcBotBase.TradersInfo,
                        UnlockedInfo = csPmcBotBase.UnlockedInfo,
                        RagfairInfo = csPmcBotBase.RagfairInfo,
                        Achievements = csPmcBotBase.Achievements,
                        RepeatableQuests = csPmcBotBase.RepeatableQuests,
                        Bonuses = csPmcBotBase.Bonuses,
                        Notes = csPmcBotBase.Notes,
                        CarExtractCounts = csPmcBotBase.CarExtractCounts,
                        CoopExtractCounts = csPmcBotBase.CoopExtractCounts,
                        SurvivorClass = csPmcBotBase.SurvivorClass,
                        WishList = csPmcBotBase.WishList,
                        MoneyTransferLimitData = csPmcBotBase.MoneyTransferLimitData,
                        IsPmc = csPmcBotBase.IsPmc,
                        Prestige = {},
                    }
                }
            };
        }

        public SptProfile GetCSFullProfile(MongoId sessionId, MongoId csPlayerSessionId)
        {
            return mcsProfileService.GetCSFullProfile(sessionId, csPlayerSessionId);
        }

        public SptProfile GetCSFullProfileByAccountId(MongoId sessionId, string csAccountId)
        {
            return mcsProfileService.GetCSFullProfileByAccountId(sessionId, csAccountId);
        }
    }
}