

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using HarmonyLib;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSProfileController(
        MCSProfileService mcsProfileService,
        NotificationSendHelper notificationSendHelper,
        PlayerScavGenerator playerScavGenerator,
        ConfigServer configServer,
        HashUtil hashUtil,
        ICloner cloner,
        BotHelper botHelper,
        ServerLocalisationService serverLocalisationService,
        ISptLogger<MCSProfileController> logger,
        BotInventoryContainerService botInventoryContainerService,
        BotLootCacheService botLootCacheService,
        BotGenerator botGenerator,
        ProfileHelper profileHelper,
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
            var csScavBotBase = GenerateCSScavProfile(bossSessionId, csFullProfile, carryServiceLevel);

            csScavBotBase.SessionId = csPmcBotBase.SessionId;
            csFullProfile.ProfileInfo.Aid = csPmcBotBase.Aid;
            csFullProfile.ProfileInfo.ScavengerId = csScavBotBase.Id;
            csFullProfile.CharacterData.PmcData.Savage = csScavBotBase.Id;
            csFullProfile.CharacterData.ScavData = csScavBotBase;

            SaveCSPlayerProfile(bossSessionId, csFullProfile);
            return csFullProfile;
        }

        private PmcData GenerateCSScavProfile(MongoId bossSessionId, SptProfile csPlayerFullProfile, int carryServiceLevel)
        {
            var profileCharactersClone = cloner.Clone(csPlayerFullProfile.CharacterData);
            var pmcDataClone = cloner.Clone(profileCharactersClone.PmcData);
            var bossFullProfile = saveServer.GetProfile(bossSessionId);
            var existingScavDataClone = cloner.Clone(bossFullProfile.CharacterData.ScavData);

            var scavKarmaLevel = carryServiceLevel + 2;
            var playerScavConfig = configServer.GetConfig<PlayerScavConfig>();

            if (
                !playerScavConfig.KarmaLevel.TryGetValue(scavKarmaLevel.ToString(CultureInfo.InvariantCulture), out var playerScavKarmaSettings)
            )
            {
                logger.Error(serverLocalisationService.GetText("scav-missing_karma_settings", scavKarmaLevel));
            }

            if (logger.IsLogEnabled(LogLevel.Debug))
            {
                logger.Debug($"Generated player scav load out with karma level: {scavKarmaLevel}");
            }

            var baseBotNode = cloner.Clone(botHelper.GetBotTemplate("assault"));

            var playerScavGeneratorTraverse = Traverse.Create(playerScavGenerator);
            playerScavGeneratorTraverse.Method("AdjustBotTemplateWithKarmaSpecificSettings", [playerScavKarmaSettings, baseBotNode]).GetValue();

            var scavData = botGenerator.GeneratePlayerScav(
                bossSessionId,
                playerScavKarmaSettings.BotTypeForLoot.ToLowerInvariant(),
                "hard",
                baseBotNode,
                pmcDataClone
            );

            playerScavGeneratorTraverse.Method("AddAdditionalLootToPlayerScavContainers", [
                scavData.Id.Value,
                playerScavKarmaSettings.LootItemsToAddChancePercent,
                scavData,
                new HashSet<EquipmentSlots>{
                    EquipmentSlots.TacticalVest, EquipmentSlots.Pockets, EquipmentSlots.Backpack
                }
            ]).GetValue();

            botInventoryContainerService.ClearCache(scavData.Id.Value);

            botLootCacheService.ClearCache();

            scavData.Savage = null;
            scavData.Aid = pmcDataClone.Aid;
            scavData.TradersInfo = pmcDataClone.TradersInfo;
            scavData.Info.Settings = new();
            scavData.Info.Bans = [];
            scavData.Info.RegistrationDate = pmcDataClone.Info.RegistrationDate;
            scavData.Info.GameVersion = pmcDataClone.Info.GameVersion;
            scavData.Info.MemberCategory = MemberCategory.UniqueId;
            scavData.Info.LockedMoveCommands = true;
            scavData.Info.MainProfileNickname = pmcDataClone.Info.Nickname;
            scavData.RagfairInfo = pmcDataClone.RagfairInfo;
            scavData.UnlockedInfo = pmcDataClone.UnlockedInfo;

            scavData.Id = existingScavDataClone.Id ?? pmcDataClone.Savage;
            scavData.SessionId = existingScavDataClone.SessionId ?? pmcDataClone.SessionId;
            scavData.Skills = existingScavDataClone.GetSkillsOrDefault();
            scavData.Stats = existingScavDataClone.Stats ?? profileHelper.GetDefaultCounters();;
            scavData.Info.Level = 1;
            scavData.Info.Experience = 200;
            scavData.Quests = existingScavDataClone.Quests ?? [];
            scavData.TaskConditionCounters = existingScavDataClone.TaskConditionCounters ?? new();
            scavData.Notes = existingScavDataClone.Notes ?? new Notes { DataNotes = [] };
            scavData.WishList = existingScavDataClone.WishList ?? new();
            scavData.Encyclopedia = pmcDataClone.Encyclopedia ?? new();
            scavData.Variables = existingScavDataClone.Variables ?? new();

            // 作为护航, 很可能反而不仅不要移除，甚至还需要放入Boss安全箱
            scavData = profileHelper.RemoveSecureContainer(scavData);
            return scavData;
        }

        private SptProfile GenerateCSFullProfile(BotBase csPmcBotBase)
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
                        Prestige = { },
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