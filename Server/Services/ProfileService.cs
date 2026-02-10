using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Models.Enums;
using MiyakoCarryService.Server.Utils;
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

        public bool RemoveMcsBotPlayerProfile(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            var file = System.IO.Path.Combine(_profileFolderDir, mcsLeadPlayerId, $"{mcsBotPlayerId}.json");
            logger.Error(string.Format(serverLocalisationService.GetText(Locales.CLEANINGUPOUTDATEDMCSPLAYERPROFILE), mcsBotPlayerId));
            if (_profiles[mcsLeadPlayerId].ContainsKey(mcsBotPlayerId))
            {
                _profiles[mcsLeadPlayerId].TryRemove(mcsBotPlayerId, out _);
                if (!fileUtil.DeleteFile(file))
                {
                    logger.Error(string.Format(serverLocalisationService.GetText(Locales.CANNOTDELETEFILENOTFOUND), file));
                }
            }

            return !fileUtil.FileExists(file);
        }

        public void ProcessExpiredMcsBotPlayerProfile(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            var mcsBotPlayerProfile = GetMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
            if (mcsBotPlayerProfile is null)
            {
                var errorInfo = serverLocalisationService.GetText(Locales.FAILEDLOADMCSPLAYERPROFILE);
                logger.Error(errorInfo);
                throw new NullReferenceException(errorInfo);
            }

            _ = new Timer(
                _ =>
                {
                    try
                    {
                        if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsLeadPlayerId))
                        {
                            var notification = notificationHelper.GenerateWsGroupMatchUserLeave(mcsBotPlayerProfile);
                            var notification2 = notificationHelper.GenerateWsFriendsListAccept(mcsBotPlayerProfile, NotificationEventType.youAreRemovedFromFriendList);
                            notificationSendHelper.SendMessage(mcsLeadPlayerId, notification);
                            notificationSendHelper.SendMessage(mcsLeadPlayerId, notification2);
                        }
                    }
                    finally
                    {
                        RemoveMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
                    }

                },
                null,
                TimeSpan.FromMicroseconds(1000),
                Timeout.InfiniteTimeSpan
            );
        }

        public void ProcessExpiredMcsBotPlayerProfiles(MongoId mcsLeadPlayerId, HashSet<MongoId> mcsBotPlayerIds)
        {
            foreach (var mcsBotPlayerId in mcsBotPlayerIds)
            {
                ProcessExpiredMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
            }
        }

        public async Task SaveMcsBotPlayerProfile(MongoId mcsLeadPlayerId, SptProfile mcsBotPlayerProfile)
        {
            var mcsBotPlayerId = mcsBotPlayerProfile.ProfileInfo.ProfileId.Value;
            var saveLock = _saveLocks.GetOrAdd(mcsBotPlayerId, _ => new(1, 1));
            await saveLock.WaitAsync();
            try
            {
                try
                {
                    var profilePath = System.IO.Path.Combine(_profileFolderDir, mcsLeadPlayerId, $"{mcsBotPlayerId}.json");
                    _profiles.GetOrAdd(mcsLeadPlayerId, _ => new ConcurrentDictionary<MongoId, SptProfile>()).GetOrAdd(mcsBotPlayerId, mcsBotPlayerProfile);
                    var jsonProfile = jsonUtil.Serialize(_profiles[mcsLeadPlayerId][mcsBotPlayerId], true);
                    await fileUtil.WriteFileAsync(profilePath, jsonProfile);
                }
                catch (Exception e)
                {
                    logger.Error(serverLocalisationService.GetText(Locales.SAVEMCSPLAYERPROFILEEXCEPTION), e);
                }
            }
            finally
            {
                saveLock.Release();
            }
        }

        private async Task LoadMcsBotPlayerProfileAsync(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            var filePath = System.IO.Path.Combine(_profileFolderDir, mcsLeadPlayerId, $"{mcsBotPlayerId}.json");
            if (fileUtil.FileExists(filePath))
            {
                var mcsBotPlayerProfile = await jsonUtil.DeserializeFromFileAsync<SptProfile>(filePath);
                if (mcsBotPlayerProfile is not null)
                {
                    _profiles.GetOrAdd(mcsLeadPlayerId, _ => new()).GetOrAdd(mcsBotPlayerId, mcsBotPlayerProfile);
                }
            }
        }

        private async Task LoadAllMcsBotPlayerProfileAsync()
        {
            if (!fileUtil.DirectoryExists(_profileFolderDir))
            {
                fileUtil.CreateDirectory(_profileFolderDir);
            }

            var mcsLeadPlayerIdsFolderPath = fileUtil.GetDirectories(_profileFolderDir);
            foreach (var mcsLeadPlayerIdFolderPath in mcsLeadPlayerIdsFolderPath)
            {
                var mcsLeadPlayerId = System.IO.Path.GetFileNameWithoutExtension(mcsLeadPlayerIdFolderPath);
                if (MongoId.IsValidMongoId(mcsLeadPlayerId))
                {
                    var mcsLeadPlayerIdProfileFolderPath = System.IO.Path.Combine(_profileFolderDir, mcsLeadPlayerIdFolderPath);
                    var files = fileUtil.GetFiles(mcsLeadPlayerIdProfileFolderPath).Where(item => fileUtil.GetFileExtension(item) == "json");
                    foreach (var file in files)
                    {
                        var mcsBotPlayerId = System.IO.Path.GetFileNameWithoutExtension(file);
                        if (MongoId.IsValidMongoId(mcsBotPlayerId))
                        {
                            if (orderInfoService.CheckMcsBotPlayerExist(mcsBotPlayerId))
                            {
                                await LoadMcsBotPlayerProfileAsync(mcsLeadPlayerId, mcsBotPlayerId);
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

        public BotBase GeneratePmcBotProfile(MongoId mcsLeadPlayerId, PmcData pmcData, EBotType botType, int carryServiceLevel)
        {
            var isCommon = botType == EBotType.common;
            var botDifficulty = (BotDifficulty)carryServiceLevel;
            var role = isCommon ? (pmcData.Info.Side == "Usec" ? "pmcUSEC" : "pmcBEAR" ): botType.ToString();
            var botBase = botGenerator.PrepareAndGenerateBot(mcsLeadPlayerId, new()
            {
                IsPmc = isCommon,
                Side = isCommon ? pmcData.Info.Side : "Savage",
                Role = role,
                PlayerLevel = 1,
                BotRelativeLevelDeltaMin = 0,
                BotRelativeLevelDeltaMax = 0,
                BotCountToGenerate = 1,
                BotDifficulty = botDifficulty <= BotDifficulty.Impossible && botDifficulty > BotDifficulty.AsOnline ? botDifficulty.ToString().ToLower() : "impossible",
                IsPlayerScav = false,
                AllPmcsHaveSameNameAsPlayer = false
            });
            
            var playerName = randomUtil.GetArrayValue(_afdianNames);
            if (playerName is not null)
            {
                botBase.Info.Nickname = playerName;
                botBase.Info.LowerNickname = playerName.ToLower();
            }

            return botBase;
        }

        public SptProfile? GetMcsBotPlayerProfile(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            if (_profiles.ContainsKey(mcsLeadPlayerId))
            {
                _profiles.TryGetValue(mcsLeadPlayerId, out var mcsBotPlayerProfiles);
                if (mcsBotPlayerProfiles is null)
                {
                    return null;
                }
                return mcsBotPlayerProfiles.FirstOrDefault(p => p.Key == mcsBotPlayerId).Value;
            }
            return null;
        }

        public SptProfile? GetMcsBotPlayerProfileByAccountId(MongoId mcsLeadPlayerId, string mcsAid)
        {
            var isInt = int.TryParse(mcsAid, out var intMcsAid);
            if (!isInt)
            {
                logger.Error(string.Format(serverLocalisationService.GetText(Locales.ACCOUNTIDISINVAILD), mcsAid));
            }

            return GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, intMcsAid);
        }

        public SptProfile? GetMcsBotPlayerProfileByAccountId(MongoId mcsLeadPlayerId, int mcsAid)
        {
            if (_profiles.ContainsKey(mcsLeadPlayerId))
            {
                _profiles.TryGetValue(mcsLeadPlayerId, out var mcsBotPlayerProfiles);
                if (mcsBotPlayerProfiles is null)
                {
                    return null;
                }
                return mcsBotPlayerProfiles.FirstOrDefault(p => p.Value.ProfileInfo.Aid == mcsAid).Value;
            }
            return null;
        }

        public List<SptProfile> GetAllMcsBotPlayerProfileByBossId(MongoId mcsLeadPlayerId)
        {
            if (_profiles.ContainsKey(mcsLeadPlayerId))
            {
                _profiles.TryGetValue(mcsLeadPlayerId, out var bossCSPlayerFullProfiles);
                if (bossCSPlayerFullProfiles is null)
                {
                    return new();
                }
                List<SptProfile> mcsBotPlayerFullProfles = [.. bossCSPlayerFullProfiles.Values];
                return mcsBotPlayerFullProfles;
            }
            return new();
        }

        public SptProfile Generate(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId, PmcData completeQuestPmcData, EBotType botType, int carryServiceLevel)
        {
            var isCommon = botType == EBotType.common;
            var mcsPmcBotBase = GeneratePmcBotProfile(mcsLeadPlayerId, completeQuestPmcData, botType, carryServiceLevel);
            mcsPmcBotBase.Info.Level = carryServiceLevel * 10 + 20 + (!isCommon ? 30 : 0);
            mcsPmcBotBase.Id = mcsBotPlayerId;
            mcsPmcBotBase.SessionId = mcsBotPlayerId;
            mcsPmcBotBase.Aid = hashUtil.GenerateAccountId();

            var mcsBotPlayerFullProfile = GenerateMcsBotPlayerFullProfile(mcsPmcBotBase);
            var mcsBotPlayerScavBotBase = isCommon ? mcsBotPlayerFullProfile.CharacterData.PmcData : GenerateMcsBotScavPlayerFullProfile(mcsLeadPlayerId, mcsBotPlayerFullProfile, carryServiceLevel);

            mcsBotPlayerScavBotBase.SessionId = mcsPmcBotBase.SessionId;
            mcsBotPlayerFullProfile.ProfileInfo.Aid = mcsPmcBotBase.Aid;
            mcsBotPlayerFullProfile.ProfileInfo.ScavengerId = mcsBotPlayerScavBotBase.Id;
            mcsBotPlayerFullProfile.CharacterData.PmcData.Savage = mcsBotPlayerScavBotBase.Id;
            mcsBotPlayerFullProfile.CharacterData.ScavData = mcsBotPlayerScavBotBase;

            _ = SaveMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerFullProfile);
            return mcsBotPlayerFullProfile;
        }

        private PmcData GenerateMcsBotScavPlayerFullProfile(MongoId mcsLeadPlayerId, SptProfile mcsBotPlayerFullProfile, int carryServiceLevel)
        {
            var profileCharactersClone = cloner.Clone(mcsBotPlayerFullProfile.CharacterData);
            var pmcDataClone = cloner.Clone(profileCharactersClone.PmcData);
            var bossFullProfile = saveServer.GetProfile(mcsLeadPlayerId);
            var existingScavDataClone = cloner.Clone(bossFullProfile.CharacterData.ScavData);

            var scavKarmaLevel = carryServiceLevel + 2;
            var playerScavConfig = configServer.GetConfig<PlayerScavConfig>();

            if (
                !playerScavConfig.KarmaLevel.TryGetValue(scavKarmaLevel.ToString(CultureInfo.InvariantCulture), out var playerScavKarmaSettings)
            )
            {
                logger.Error(serverLocalisationService.GetText("scav-missing_karma_settings", scavKarmaLevel));
            }

            var baseBotNode = cloner.Clone(botHelper.GetBotTemplate("assault"));

            var playerScavGeneratorTraverse = Traverse.Create(playerScavGenerator);
            playerScavGeneratorTraverse.Method("AdjustBotTemplateWithKarmaSpecificSettings", [playerScavKarmaSettings, baseBotNode]).GetValue();

            var botDifficulty = (BotDifficulty)carryServiceLevel;

            var botBase = botGenerator.PrepareAndGenerateBot(mcsLeadPlayerId, new()
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
                Id = new(),
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

            scavData.Info.Settings = new();
            scavData.Info.Bans = [];
            scavData.Info.RegistrationDate = pmcDataClone.Info.RegistrationDate;
            scavData.Info.GameVersion = pmcDataClone.Info.GameVersion;
            scavData.Info.MemberCategory = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.MemberCategory;
            scavData.Info.SelectedMemberCategory = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.SelectedMemberCategory;
            scavData.Info.LockedMoveCommands = true;
            scavData.Info.MainProfileNickname = pmcDataClone.Info.Nickname;
            scavData.Info.Level = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Level;
            scavData.Info.Experience = mcsBotPlayerFullProfile.CharacterData.PmcData.Info.Experience;
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