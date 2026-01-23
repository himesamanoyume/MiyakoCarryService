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
    public sealed class ProfileService(
        SaveServer saveServer,
        JsonUtil jsonUtil,
        FileUtil fileUtil,
        NotificationSendHelper notificationSendHelper,
        ISptLogger<ProfileService> logger,
        ConfigService configService,
        RandomUtil randomUtil,
        DatabaseService databaseService,
        HashUtil hashUtil,
        ICloner cloner,
        ConfigServer configServer,
        BotHelper botHelper,
        ServerLocalisationService serverLocalisationService,
        NotificationHelper notificationHelper,
        BotInventoryContainerService botInventoryContainerService,
        BotLootCacheService botLootCacheService,
        BotGenerator botGenerator,
        PlayerScavGenerator playerScavGenerator,
        ProfileHelper profileHelper
    )
    {
        private readonly string _profileFolderDir = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "profiles");
        private readonly string _afdianFolderDir = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "bots", "types");

        private readonly ConcurrentDictionary<MongoId, ConcurrentDictionary<MongoId, SptProfile>> _profiles = new();
        private List<string> _afdianNames = [];

        public bool RemoveProfile(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId)
        {
            var file = System.IO.Path.Combine(_profileFolderDir, mcsBossPlayerId, $"{mcsBotPlayerId}.json");
            if (_profiles[mcsBossPlayerId].ContainsKey(mcsBotPlayerId))
            {
                _profiles[mcsBossPlayerId].TryRemove(mcsBotPlayerId, out _);
                if (!fileUtil.DeleteFile(file))
                {
                    logger.Error($"Unable to delete file, not found: {file}");
                }
            }

            return !fileUtil.FileExists(file);
        }

        public void ProcessExpiredMcsBotPlayerProfile(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId)
        {
            var mcsBotPlayerProfile = GetMcsBotPlayerProfile(mcsBossPlayerId, mcsBotPlayerId);
            _ = new Timer(
                _ =>
                {
                    var notification = notificationHelper.GenerateWsGroupMatchUserLeave(mcsBotPlayerProfile);
                    notificationSendHelper.SendMessage(mcsBossPlayerId, notification);

                    var notification2 = notificationHelper.GenerateWsFriendsListAccept(mcsBotPlayerProfile, NotificationEventType.youAreRemovedFromFriendList);
                    notificationSendHelper.SendMessage(mcsBossPlayerId, notification2);
                    RemoveProfile(mcsBossPlayerId, mcsBotPlayerId);
                },
                null,
                TimeSpan.FromMicroseconds(1000),
                Timeout.InfiniteTimeSpan
            );
        }

        public void ProcessExpiredMcsBotPlayerProfiles(MongoId mcsBossPlayerId, HashSet<MongoId> mcsBotPlayerIds)
        {
            foreach (var mcsBotPlayerId in mcsBotPlayerIds)
            {
                ProcessExpiredMcsBotPlayerProfile(mcsBossPlayerId, mcsBotPlayerId);
            }
        }

        public void SaveMcsBotPlayerProfile(MongoId mcsBossPlayerId, SptProfile mcsBotPlayerProfile)
        {
            Console.WriteLine("保存护航存档");
            var mcsBotPlayerId = (MongoId)mcsBotPlayerProfile.ProfileInfo.ProfileId;
            var profilePath = System.IO.Path.Combine(_profileFolderDir, mcsBossPlayerId, $"{mcsBotPlayerId}.json");
            _profiles.GetOrAdd(mcsBossPlayerId, _ => new ConcurrentDictionary<MongoId, SptProfile>()).GetOrAdd(mcsBotPlayerId, mcsBotPlayerProfile);
            var jsonProfile = jsonUtil.Serialize(_profiles[mcsBossPlayerId][mcsBotPlayerId], true);
            fileUtil.WriteFile(profilePath, jsonProfile);
        }

        private async Task LoadMcsBotPlayerProfileAsync(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId)
        {
            var filePath = System.IO.Path.Combine(_profileFolderDir, mcsBossPlayerId, $"{mcsBotPlayerId}.json");
            if (fileUtil.FileExists(filePath))
            {
                var mcsBotPlayerProfile = await jsonUtil.DeserializeFromFileAsync<SptProfile>(filePath);
                if (mcsBotPlayerProfile is not null)
                {
                    _profiles.GetOrAdd(mcsBossPlayerId, _ => new()).GetOrAdd(mcsBotPlayerId, mcsBotPlayerProfile);
                }
            }
        }

        private async Task LoadAllMcsBotPlayerProfileAsync()
        {
            if (!fileUtil.DirectoryExists(_profileFolderDir))
            {
                fileUtil.CreateDirectory(_profileFolderDir);
            }

            var sessionIdsFolderPath = fileUtil.GetDirectories(_profileFolderDir);
            foreach (var sessionIdFolderPath in sessionIdsFolderPath)
            {
                var sessionId = System.IO.Path.GetFileNameWithoutExtension(sessionIdFolderPath);
                var mcsBotPlayerProfileFolderPath = System.IO.Path.Combine(_profileFolderDir, sessionIdFolderPath);
                var files = fileUtil.GetFiles(mcsBotPlayerProfileFolderPath).Where(item => fileUtil.GetFileExtension(item) == "json");
                foreach (var file in files)
                {
                    var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                    if (MongoId.IsValidMongoId(filename))
                    {
                        await LoadMcsBotPlayerProfileAsync(sessionId, filename);
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

        public BotBase GeneratePmcBotProfile(MongoId mcsBossPlayerId, PmcData pmcData, int carryServiceLevel)
        {
            var playerName = randomUtil.GetArrayValue(_afdianNames);
            var bots = databaseService.GetBots().Types;
            var pmcNames = new List<string>();
            pmcNames.AddRange(bots["usec"].FirstNames);
            pmcNames.AddRange(bots["bear"].FirstNames);

            var botBase = botGenerator.PrepareAndGenerateBot(mcsBossPlayerId, new()
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

        public SptProfile? GetMcsBotPlayerProfile(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId)
        {
            return _profiles[mcsBossPlayerId][mcsBotPlayerId];
        }

        public SptProfile? GetMcsBotPlayerProfileByAccountId(MongoId mcsBossPlayerId, string mcsAid)
        {
            var isInt = int.TryParse(mcsAid, out var intMcsAid);
            if (!isInt)
            {
                logger.Error($"Account {mcsAid} does not exist");
            }

            return GetMcsBotPlayerProfileByAccountId(mcsBossPlayerId, intMcsAid);
        }

        public SptProfile? GetMcsBotPlayerProfileByAccountId(MongoId mcsBossPlayerId, int mcsAid)
        {
            if (_profiles.ContainsKey(mcsBossPlayerId))
            {
                _profiles.TryGetValue(mcsBossPlayerId, out var bossCSPlayerFullProfiles);
                if (bossCSPlayerFullProfiles is null)
                {
                    return null;
                }
                return bossCSPlayerFullProfiles.FirstOrDefault(p => p.Value.ProfileInfo.Aid == mcsAid).Value;
            }
            return null;
        }

        public List<SptProfile> GetAllMcsBotPlayerProfileByBossId(MongoId mcsBossPlayerId)
        {
            if (_profiles.ContainsKey(mcsBossPlayerId))
            {
                _profiles.TryGetValue(mcsBossPlayerId, out var bossCSPlayerFullProfiles);
                if (bossCSPlayerFullProfiles is null)
                {
                    return new();
                }
                List<SptProfile> mcsBotPlayerFullProfles = [.. bossCSPlayerFullProfiles.Values];
                return mcsBotPlayerFullProfles;
            }
            return new();
        }

        public SptProfile Generate(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId, PmcData completeQuestPmcData, int carryServiceLevel)
        {
            var mcsPmcBotBase = GeneratePmcBotProfile(mcsBossPlayerId, completeQuestPmcData, carryServiceLevel);
            mcsPmcBotBase.Id = mcsBotPlayerId;
            mcsPmcBotBase.SessionId = mcsBotPlayerId;
            mcsPmcBotBase.Aid = hashUtil.GenerateAccountId();

            var mcsBotPlayerFullProfile = GenerateMcsBotPlayerFullProfile(mcsPmcBotBase);
            var mcsBotPlayerScavBotBase = GenerateMcsBotScavPlayerFullProfile(mcsBossPlayerId, mcsBotPlayerFullProfile, carryServiceLevel);

            mcsBotPlayerScavBotBase.SessionId = mcsPmcBotBase.SessionId;
            mcsBotPlayerFullProfile.ProfileInfo.Aid = mcsPmcBotBase.Aid;
            mcsBotPlayerFullProfile.ProfileInfo.ScavengerId = mcsBotPlayerScavBotBase.Id;
            mcsBotPlayerFullProfile.CharacterData.PmcData.Savage = mcsBotPlayerScavBotBase.Id;
            mcsBotPlayerFullProfile.CharacterData.ScavData = mcsBotPlayerScavBotBase;

            SaveMcsBotPlayerProfile(mcsBossPlayerId, mcsBotPlayerFullProfile);
            return mcsBotPlayerFullProfile;
        }

        private PmcData GenerateMcsBotScavPlayerFullProfile(MongoId mcsBossPlayerId, SptProfile mcsBotPlayerFullProfile, int carryServiceLevel)
        {
            var profileCharactersClone = cloner.Clone(mcsBotPlayerFullProfile.CharacterData);
            var pmcDataClone = cloner.Clone(profileCharactersClone.PmcData);
            var bossFullProfile = saveServer.GetProfile(mcsBossPlayerId);
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
                mcsBossPlayerId,
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

        private SptProfile GenerateMcsBotPlayerFullProfile(BotBase mcsBotPlayerPmcBotBase)
        {
            return new SptProfile
            {
                ProfileInfo = new SPTarkov.Server.Core.Models.Eft.Profile.Info
                {
                    ProfileId = mcsBotPlayerPmcBotBase.SessionId,
                    Username = mcsBotPlayerPmcBotBase.Info.Nickname
                },
                CharacterData = new Characters
                {
                    PmcData = new PmcData
                    {
                        Id = mcsBotPlayerPmcBotBase.Id,
                        Aid = mcsBotPlayerPmcBotBase.Aid,
                        SessionId = mcsBotPlayerPmcBotBase.SessionId,
                        KarmaValue = mcsBotPlayerPmcBotBase.KarmaValue,
                        Info = mcsBotPlayerPmcBotBase.Info,
                        Customization = mcsBotPlayerPmcBotBase.Customization,
                        Health = mcsBotPlayerPmcBotBase.Health,
                        Inventory = mcsBotPlayerPmcBotBase.Inventory,
                        Skills = mcsBotPlayerPmcBotBase.Skills,
                        Stats = mcsBotPlayerPmcBotBase.Stats,
                        Encyclopedia = mcsBotPlayerPmcBotBase.Encyclopedia,
                        TaskConditionCounters = mcsBotPlayerPmcBotBase.TaskConditionCounters,
                        InsuredItems = mcsBotPlayerPmcBotBase.InsuredItems,
                        Hideout = mcsBotPlayerPmcBotBase.Hideout,
                        Quests = mcsBotPlayerPmcBotBase.Quests,
                        TradersInfo = mcsBotPlayerPmcBotBase.TradersInfo,
                        UnlockedInfo = mcsBotPlayerPmcBotBase.UnlockedInfo,
                        RagfairInfo = mcsBotPlayerPmcBotBase.RagfairInfo,
                        Achievements = mcsBotPlayerPmcBotBase.Achievements,
                        RepeatableQuests = mcsBotPlayerPmcBotBase.RepeatableQuests,
                        Bonuses = mcsBotPlayerPmcBotBase.Bonuses,
                        Notes = mcsBotPlayerPmcBotBase.Notes,
                        CarExtractCounts = mcsBotPlayerPmcBotBase.CarExtractCounts,
                        CoopExtractCounts = mcsBotPlayerPmcBotBase.CoopExtractCounts,
                        SurvivorClass = mcsBotPlayerPmcBotBase.SurvivorClass,
                        WishList = mcsBotPlayerPmcBotBase.WishList,
                        MoneyTransferLimitData = mcsBotPlayerPmcBotBase.MoneyTransferLimitData,
                        IsPmc = mcsBotPlayerPmcBotBase.IsPmc,
                        Prestige = { },
                    }
                }
            };
        }

        public async Task OnPostLoadAsync()
        {
            await LoadAllMcsBotPlayerProfileAsync();
            await LoadAfdianPmcName();
        }
    }
}