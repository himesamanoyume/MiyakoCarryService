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
using SPTarkov.Server.Core.Models.Enums.RaidSettings;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Servers.Ws;
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
        SptWebSocketConnectionHandler sptWebSocketConnectionHandler,
        ProfileHelper profileHelper,
        OrderInfoService orderInfoService
    )
    {
        private readonly string _profileFolderDir = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "profiles");
        private readonly string _afdianFolderDir = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "bots", "types");

        private readonly ConcurrentDictionary<MongoId, ConcurrentDictionary<MongoId, SptProfile>> _profiles = new();
        private readonly ConcurrentDictionary<MongoId, SemaphoreSlim> _saveLocks = new();
        private List<string> _afdianNames = [];

        public bool RemoveMcsBotPlayerProfile(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId)
        {
            var file = System.IO.Path.Combine(_profileFolderDir, mcsBossPlayerId, $"{mcsBotPlayerId}.json");
            logger.Error($"正在删除 {mcsBotPlayerId}.json");
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
            if (mcsBotPlayerProfile is null)
            {
                var errorInfo = "没能获取到护航玩家存档";
                logger.Error(errorInfo);
                throw new NullReferenceException(errorInfo);
            }

            _ = new Timer(
                _ =>
                {
                    try
                    {
                        if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsBossPlayerId))
                        {
                            var notification = notificationHelper.GenerateWsGroupMatchUserLeave(mcsBotPlayerProfile);
                            var notification2 = notificationHelper.GenerateWsFriendsListAccept(mcsBotPlayerProfile, NotificationEventType.youAreRemovedFromFriendList);
                            notificationSendHelper.SendMessage(mcsBossPlayerId, notification);
                            notificationSendHelper.SendMessage(mcsBossPlayerId, notification2);
                        }
                    }
                    finally
                    {
                        RemoveMcsBotPlayerProfile(mcsBossPlayerId, mcsBotPlayerId);
                    }

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

        public async Task SaveMcsBotPlayerProfile(MongoId mcsBossPlayerId, SptProfile mcsBotPlayerProfile)
        {
            var mcsBotPlayerId = mcsBotPlayerProfile.ProfileInfo.ProfileId.Value;
            var saveLock = _saveLocks.GetOrAdd(mcsBotPlayerId, _ => new(1, 1));
            await saveLock.WaitAsync();
            try
            {
                try
                {
                    var profilePath = System.IO.Path.Combine(_profileFolderDir, mcsBossPlayerId, $"{mcsBotPlayerId}.json");
                    _profiles.GetOrAdd(mcsBossPlayerId, _ => new ConcurrentDictionary<MongoId, SptProfile>()).GetOrAdd(mcsBotPlayerId, mcsBotPlayerProfile);
                    var jsonProfile = jsonUtil.Serialize(_profiles[mcsBossPlayerId][mcsBotPlayerId], true);
                    await fileUtil.WriteFileAsync(profilePath, jsonProfile);
                }
                catch (Exception e)
                {
                    logger.Error("保存护航存档异常", e);
                }
            }
            finally
            {
                saveLock.Release();
            }
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

            var mcsBossPlayerIdsFolderPath = fileUtil.GetDirectories(_profileFolderDir);
            foreach (var mcsBossPlayerIdFolderPath in mcsBossPlayerIdsFolderPath)
            {
                var mcsBossPlayerId = System.IO.Path.GetFileNameWithoutExtension(mcsBossPlayerIdFolderPath);
                if (MongoId.IsValidMongoId(mcsBossPlayerId))
                {
                    var mcsBossPlayerIdProfileFolderPath = System.IO.Path.Combine(_profileFolderDir, mcsBossPlayerIdFolderPath);
                    var files = fileUtil.GetFiles(mcsBossPlayerIdProfileFolderPath).Where(item => fileUtil.GetFileExtension(item) == "json");
                    foreach (var file in files)
                    {
                        var mcsBotPlayerId = System.IO.Path.GetFileNameWithoutExtension(file);
                        if (MongoId.IsValidMongoId(mcsBotPlayerId))
                        {
                            if (orderInfoService.CheckMcsBotPlayerExist(mcsBotPlayerId))
                            {
                                logger.Info($"加载订单中存在的 {mcsBotPlayerId} 存档");
                                await LoadMcsBotPlayerProfileAsync(mcsBossPlayerId, mcsBotPlayerId);
                            }
                        }
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
            var botDifficulty = (BotDifficulty)carryServiceLevel;

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
                BotDifficulty = botDifficulty <= BotDifficulty.Impossible && botDifficulty > BotDifficulty.AsOnline ? botDifficulty.ToString().ToLower() : "impossible",
                IsPlayerScav = false,
                AllPmcsHaveSameNameAsPlayer = false
            });
            return botBase;
        }

        public SptProfile? GetMcsBotPlayerProfile(MongoId mcsBossPlayerId, MongoId mcsBotPlayerId)
        {
            if (_profiles.ContainsKey(mcsBossPlayerId))
            {
                _profiles.TryGetValue(mcsBossPlayerId, out var mcsBotPlayerProfiles);
                if (mcsBotPlayerProfiles is null)
                {
                    logger.Error("mcsBotPlayerProfiles为空");
                    return null;
                }
                return mcsBotPlayerProfiles.FirstOrDefault(p => p.Key == mcsBotPlayerId).Value;
            }
            logger.Error($"_profiles没有key {mcsBossPlayerId}");
            return null;
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
                _profiles.TryGetValue(mcsBossPlayerId, out var mcsBotPlayerProfiles);
                if (mcsBotPlayerProfiles is null)
                {
                    return null;
                }
                return mcsBotPlayerProfiles.FirstOrDefault(p => p.Value.ProfileInfo.Aid == mcsAid).Value;
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

            _ = SaveMcsBotPlayerProfile(mcsBossPlayerId, mcsBotPlayerFullProfile);
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

            // 此处会生成玩家Scav的数据，没有安全箱
            // var scavData = botGenerator.GeneratePlayerScav(
            //     mcsBossPlayerId,
            //     playerScavKarmaSettings.BotTypeForLoot.ToLowerInvariant(),
            //     "hard",
            //     baseBotNode,
            //     pmcDataClone
            // );

            // 以AI Scav的形式生成，带有安全箱
            var botDifficulty = (BotDifficulty)carryServiceLevel;

            var botBase = botGenerator.PrepareAndGenerateBot(mcsBossPlayerId, new()
            {
                IsPmc = false,
                Side = "Savage",
                Role = playerScavKarmaSettings.BotTypeForLoot.ToLowerInvariant(),
                PlayerLevel = carryServiceLevel * 10 + 20,
                BotRelativeLevelDeltaMin = 0,
                BotRelativeLevelDeltaMax = 0,
                BotCountToGenerate = 1,
                BotDifficulty = botDifficulty <= BotDifficulty.Impossible && botDifficulty > BotDifficulty.AsOnline ? botDifficulty.ToString().ToLower() : "impossible",
                IsPlayerScav = false,
                ClearBotContainerCacheAfterGeneration = false
            });

            var scavData = new PmcData
            {
                Id = existingScavDataClone.Id ?? pmcDataClone.Savage,
                Aid = pmcDataClone.Aid,
                SessionId = existingScavDataClone.SessionId ?? pmcDataClone.SessionId,
                Savage = null,
                KarmaValue = botBase.KarmaValue,
                Info = botBase.Info,
                Customization = botBase.Customization,
                Health = botBase.Health,
                Inventory = botBase.Inventory,
                Skills = existingScavDataClone.GetSkillsOrDefault(),
                Stats = existingScavDataClone.Stats ?? profileHelper.GetDefaultCounters(),
                Encyclopedia = botBase.Encyclopedia,
                TaskConditionCounters = botBase.TaskConditionCounters,
                InsuredItems = botBase.InsuredItems,
                Hideout = botBase.Hideout,
                Quests = botBase.Quests,
                TradersInfo = pmcDataClone.TradersInfo,
                UnlockedInfo = pmcDataClone.UnlockedInfo,
                RagfairInfo = pmcDataClone.RagfairInfo,
                Achievements = botBase.Achievements,
                RepeatableQuests = botBase.RepeatableQuests,
                Bonuses = botBase.Bonuses,
                Notes = botBase.Notes,
                CarExtractCounts = botBase.CarExtractCounts,
                CoopExtractCounts = botBase.CoopExtractCounts,
                SurvivorClass = botBase.SurvivorClass,
                WishList = botBase.WishList,
                MoneyTransferLimitData = botBase.MoneyTransferLimitData,
                IsPmc = botBase.IsPmc,
                Variables = existingScavDataClone.Variables ?? new(),
                Prestige = new Dictionary<string, long>()
            };

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

            // scavData.Savage = null;
            // scavData.Aid = pmcDataClone.Aid;
            // scavData.TradersInfo = pmcDataClone.TradersInfo;
            scavData.Info.Settings = new();
            scavData.Info.Bans = [];
            scavData.Info.RegistrationDate = pmcDataClone.Info.RegistrationDate;
            scavData.Info.GameVersion = pmcDataClone.Info.GameVersion;
            scavData.Info.MemberCategory = MemberCategory.UniqueId;
            scavData.Info.LockedMoveCommands = true;
            scavData.Info.MainProfileNickname = pmcDataClone.Info.Nickname;
            // scavData.RagfairInfo = pmcDataClone.RagfairInfo;
            // scavData.UnlockedInfo = pmcDataClone.UnlockedInfo;

            // scavData.Id = existingScavDataClone.Id ?? pmcDataClone.Savage;
            // scavData.SessionId = existingScavDataClone.SessionId ?? pmcDataClone.SessionId;
            // scavData.Skills = existingScavDataClone.GetSkillsOrDefault();
            // scavData.Stats = existingScavDataClone.Stats ?? profileHelper.GetDefaultCounters();
            scavData.Info.Level = 1;
            scavData.Info.Experience = 200;
            // scavData.Quests = existingScavDataClone.Quests ?? [];
            // scavData.TaskConditionCounters = existingScavDataClone.TaskConditionCounters ?? new();
            // scavData.Notes = existingScavDataClone.Notes ?? new Notes { DataNotes = [] };
            // scavData.WishList = existingScavDataClone.WishList ?? new();
            // scavData.Encyclopedia = pmcDataClone.Encyclopedia ?? new();
            // scavData.Variables = existingScavDataClone.Variables ?? new();

            // 作为护航, 很可能反而不仅不要移除，甚至还需要放入Boss安全箱
            // scavData = profileHelper.RemoveSecureContainer(scavData);
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