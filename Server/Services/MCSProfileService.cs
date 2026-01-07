using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Server.Helper;
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

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class MCSProfileService(
        SaveServer saveServer,
        JsonUtil jsonUtil,
        FileUtil fileUtil,
        NotificationSendHelper notificationSendHelper,
        ISptLogger<MCSProfileService> logger,
        MCSConfigService mcsConfigService,
        RandomUtil randomUtil,
        DatabaseService databaseService,
        HashUtil hashUtil,
        ICloner cloner,
        ConfigServer configServer,
        BotHelper botHelper,
        ServerLocalisationService serverLocalisationService,
        MCSNotificationHelper mcsNotificationHelper,
        BotInventoryContainerService botInventoryContainerService,
        BotLootCacheService botLootCacheService,
        BotGenerator botGenerator,
        PlayerScavGenerator playerScavGenerator,
        ProfileHelper profileHelper
    )
    {
        private readonly string _profileFolderDir = System.IO.Path.Join(mcsConfigService.GetModPath(), "Assets", "database", "profiles");
        private readonly string _afdianFolderDir = System.IO.Path.Join(mcsConfigService.GetModPath(), "Assets", "database", "bots", "types");

        private readonly ConcurrentDictionary<MongoId, ConcurrentDictionary<MongoId, SptProfile>> _profiles = new();
        private List<string> _afdianNames = [];

        public bool RemoveProfile(MongoId sessionId, MongoId csPlayerSessionId)
        {
            var file = System.IO.Path.Combine(_profileFolderDir, sessionId, $"{csPlayerSessionId}.json");
            if (_profiles[sessionId].ContainsKey(csPlayerSessionId))
            {
                _profiles[sessionId].TryRemove(csPlayerSessionId, out _);
                if (!fileUtil.DeleteFile(file))
                {
                    logger.Error($"Unable to delete file, not found: {file}");
                }
            }

            return !fileUtil.FileExists(file);
        }

        public void RemoveCarryServiceProfile(MongoId sessionId, MongoId csPlayerSessionId)
        {
            var completeQuestPlayerFullProfile = saveServer.GetProfile(sessionId);
            completeQuestPlayerFullProfile?.FriendProfileIds?.Remove(csPlayerSessionId);
        }

        public void ProcessExpiredCarryServiceProfile(MongoId bossSessionId, MongoId csPlayerSessionId)
        {
            RemoveCarryServiceProfile(bossSessionId, csPlayerSessionId);
            var csFullProfile = GetCSFullProfile(bossSessionId, csPlayerSessionId);
            _ = new Timer(
                _ =>
                {
                    var notification = mcsNotificationHelper.GenerateWsGroupMatchUserLeave(csFullProfile);
                    notificationSendHelper.SendMessage(bossSessionId, notification);

                    var notification2 = mcsNotificationHelper.GenerateWsFriendsListAccept(csFullProfile, NotificationEventType.youAreRemovedFromFriendList);
                    notificationSendHelper.SendMessage(bossSessionId, notification2);
                    RemoveProfile(bossSessionId, csPlayerSessionId);

                },
                null,
                TimeSpan.FromMicroseconds(1000),
                Timeout.InfiniteTimeSpan
            );
        }

        public void SaveCSPlayerProfile(MongoId sessionId, SptProfile csProfile)
        {
            Console.WriteLine("保存护航存档");
            var csPlayerSessionId = (MongoId)csProfile.ProfileInfo.ProfileId;
            var profilePath = System.IO.Path.Combine(_profileFolderDir, sessionId, $"{csPlayerSessionId}.json");
            _profiles.GetOrAdd(sessionId, _ => new ConcurrentDictionary<MongoId, SptProfile>()).GetOrAdd(csPlayerSessionId, csProfile);
            var jsonProfile = jsonUtil.Serialize(_profiles[sessionId][csPlayerSessionId], true);
            fileUtil.WriteFile(profilePath, jsonProfile);
        }

        private async Task LoadCSPlayerProfileAsync(MongoId sessionId, MongoId csPlayerSessionId)
        {
            var filePath = System.IO.Path.Combine(_profileFolderDir, sessionId, $"{csPlayerSessionId}.json");
            if (fileUtil.FileExists(filePath))
            {
                var csFullProfile = await jsonUtil.DeserializeFromFileAsync<SptProfile>(filePath);
                if (csFullProfile is not null)
                {
                    _profiles.GetOrAdd(sessionId, _ => new ConcurrentDictionary<MongoId, SptProfile>()).GetOrAdd(csPlayerSessionId, csFullProfile);
                }
            }
        }

        private async Task LoadAllCSPlayerProfileAsync()
        {
            if (!fileUtil.DirectoryExists(_profileFolderDir))
            {
                fileUtil.CreateDirectory(_profileFolderDir);
            }

            var sessionIdsFolderPath = fileUtil.GetDirectories(_profileFolderDir);
            foreach (var sessionIdFolderPath in sessionIdsFolderPath)
            {
                var sessionId = System.IO.Path.GetFileNameWithoutExtension(sessionIdFolderPath);
                var csPlayerProfileFolderPath = System.IO.Path.Combine(_profileFolderDir, sessionIdFolderPath);
                var files = fileUtil.GetFiles(csPlayerProfileFolderPath).Where(item => fileUtil.GetFileExtension(item) == "json");
                foreach (var file in files)
                {
                    var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                    if (MongoId.IsValidMongoId(filename))
                    {
                        await LoadCSPlayerProfileAsync(sessionId, filename);
                    }
                }
            }
        }

        private async Task LoadAfdianPmcName()
        {
            if (!fileUtil.DirectoryExists(_afdianFolderDir))
            {
                fileUtil.CreateDirectory(_afdianFolderDir);
            }

            var files = fileUtil.GetFiles(_afdianFolderDir).Where(item => fileUtil.GetFileExtension(item) == "json");
            foreach (var file in files)
            {
                _afdianNames = await jsonUtil.DeserializeFromFileAsync<List<string>>(file);
            }
        }

        public BotBase GeneratePmcBotProfile(MongoId sessionId, PmcData pmcData, int carryServiceLevel)
        {
            var playerName = randomUtil.GetArrayValue(_afdianNames);
            var bots = databaseService.GetBots().Types;
            var pmcNames = new List<string>();
            pmcNames.AddRange(bots["usec"].FirstNames);
            pmcNames.AddRange(bots["bear"].FirstNames);

            var botBase = botGenerator.PrepareAndGenerateBot(sessionId, new()
            {
                IsPmc = true,
                Side = pmcData.Info.Side,
                Role = pmcData.Info.Side == "Usec" ? "pmcUSEC" : "pmcBEAR",
                PlayerLevel = carryServiceLevel * 10 + 20,
                PlayerName = playerName is null ? randomUtil.GetArrayValue(pmcNames) : playerName,
                BotRelativeLevelDeltaMin = 0,
                BotRelativeLevelDeltaMax = 0,
                BotCountToGenerate = 1,
                BotDifficulty = "hard",
                IsPlayerScav = false,
                AllPmcsHaveSameNameAsPlayer = false
            });
            return botBase;
        }

        public SptProfile? GetCSFullProfile(MongoId sessionId, MongoId csPlayerSessionId)
        {
            return _profiles[sessionId][csPlayerSessionId];
        }

        public SptProfile? GetCSFullProfileByAccountId(MongoId bossSessionId, string csAccountId)
        {
            var check = int.TryParse(csAccountId, out var aid);
            if (!check)
            {
                logger.Error($"Account {csAccountId} does not exist");
            }

            return GetCSFullProfileByAccountId(bossSessionId, aid);
        }

        public SptProfile? GetCSFullProfileByAccountId(MongoId sessionId, int aid)
        {
            if (_profiles.ContainsKey(sessionId))
            {
                _profiles.TryGetValue(sessionId, out var bossCSPlayerFullProfiles);
                if (bossCSPlayerFullProfiles is null)
                {
                    return null;
                }
                return bossCSPlayerFullProfiles.FirstOrDefault(p => p.Value.ProfileInfo.Aid == aid).Value;
            }
            return null;
        }

        public SptProfile Generate(MongoId bossSessionId, MongoId csPlayerSessionId, PmcData completeQuestPmcData, int carryServiceLevel)
        {
            var csPmcBotBase = GeneratePmcBotProfile(bossSessionId, completeQuestPmcData, carryServiceLevel);
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
            scavData.Stats = existingScavDataClone.Stats ?? profileHelper.GetDefaultCounters(); ;
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

        public async Task OnPostLoadAsync()
        {
            await LoadAllCSPlayerProfileAsync();
            await LoadAfdianPmcName();
        }
    }
}