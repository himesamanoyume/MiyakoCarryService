using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Server.ChatBot;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.RaidSettings;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Servers.Ws;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Logger;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class ProfileService(
        JsonUtil jsonUtil,
        FileUtil fileUtil,
        NotificationSendHelper notificationSendHelper,
        SptLogger<ProfileService> logger,
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
        BotNameService botNameService,
        MailSendService mailSendService,
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

        public void TeamKillPunish(MongoId mcsLeadPlayerId)
        {
            var mcsBotPlayerIds = orderInfoService.SetAllOrderInfosToExpire(mcsLeadPlayerId);
            foreach (var kvp in mcsBotPlayerIds)
            {
                ProcessExpiredMcsBotPlayerProfiles(kvp.Key, kvp.Value);
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

        public SptProfile? GetMcsBotPlayerProfileByBotId(MongoId mcsBotPlayerId)
        {
            foreach (var kvp in _profiles.Values)
            {
                foreach (var profile in kvp.Values)
                {
                    if (profile.CharacterData.PmcData.SessionId.Value == mcsBotPlayerId)
                    {
                        return profile;
                    }
                }
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

        public SptProfile Generate(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId, PmcData completeQuestPmcData, OrderInfo orderInfo)
        {
            var isPmc = orderInfo.SpawnType.WildSpawnType is "common" or "pmcUSEC" or "pmcBEAR";
            var botDifficulty = (BotDifficulty)orderInfo.CarryServiceLevel;
            var role = isPmc ? (completeQuestPmcData.Info.Side == "Usec" ? "pmcUSEC" : "pmcBEAR") : orderInfo.SpawnType.WildSpawnType;
            var botGenerationDetails = new BotGenerationDetails()
            {
                IsPmc = isPmc,
                Side = isPmc ? completeQuestPmcData.Info.Side : "Savage",
                Role = role,
                PlayerLevel = orderInfo.CarryServiceLevel * 10 + 20 + (orderInfo.SpawnType.IsBoss ? 30 : 0),
                BotRelativeLevelDeltaMin = 0,
                BotRelativeLevelDeltaMax = 0,
                BotCountToGenerate = 1,
                BotDifficulty = botDifficulty <= BotDifficulty.Impossible && botDifficulty > BotDifficulty.AsOnline ? botDifficulty.ToString().ToLower() : "impossible",
                IsPlayerScav = false,
                AllPmcsHaveSameNameAsPlayer = false
            };

            // 适配APBS重复添加Tier
            var clonedBotGenerationDetails = cloner.Clone(botGenerationDetails);

            PmcData pmcData;
            try
            {
                pmcData = GeneratePmcData(mcsLeadPlayerId, mcsBotPlayerId, botGenerationDetails);
            
            }
            catch (Exception e)
            {
                var msg = string.Format(serverLocalisationService.GetText(Locales.GENERATEPROFILEERROR), botGenerationDetails.Role);
                logger.Error(msg, e);

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    mcsLeadPlayerId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    msg + $"\n{e.Message}",
                    null
                );
                botGenerationDetails.Side = completeQuestPmcData.Info.Side;
                botGenerationDetails.Role = completeQuestPmcData.Info.Side == "Usec" ? "pmcUSEC" : "pmcBEAR";
                pmcData = GeneratePmcData(mcsLeadPlayerId, mcsBotPlayerId, botGenerationDetails);
            }

            pmcData.Info.Level = botGenerationDetails.PlayerLevel;

            PmcData scavData;
            try
            {
                scavData = isPmc ? GenerateScavData(mcsLeadPlayerId, orderInfo.CarryServiceLevel, clonedBotGenerationDetails, pmcData) : GenerateScavData(pmcData, clonedBotGenerationDetails);
            }
            catch (Exception e)
            {
                var msg = string.Format(serverLocalisationService.GetText(Locales.GENERATEPROFILEERROR), clonedBotGenerationDetails.Role);
                logger.Error(msg, e);

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    mcsLeadPlayerId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    msg + $"\n{e.Message}",
                    null
                );

                clonedBotGenerationDetails.Role = "assault";
                scavData = isPmc ? GenerateScavData(mcsLeadPlayerId, orderInfo.CarryServiceLevel, clonedBotGenerationDetails, pmcData) : GenerateScavData(pmcData, clonedBotGenerationDetails);
            }
            
            scavData.Info.Settings = new();
            scavData.Info.Bans = [];
            scavData.Info.RegistrationDate = pmcData.Info.RegistrationDate;
            scavData.Info.GameVersion = pmcData.Info.GameVersion;
            scavData.Info.MemberCategory = pmcData.Info.MemberCategory;
            scavData.Info.SelectedMemberCategory = pmcData.Info.SelectedMemberCategory;
            scavData.Info.LockedMoveCommands = true;
            scavData.Info.MainProfileNickname = pmcData.Info.Nickname;
            scavData.Info.Level = pmcData.Info.Level;
            scavData.Info.Experience = pmcData.Info.Experience;

            var fullProfile = GenerateFullProfile(pmcData, scavData);
            _ = SaveMcsBotPlayerProfile(mcsLeadPlayerId, fullProfile);
            return fullProfile;
        }

        private PmcData GeneratePmcData(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId, BotGenerationDetails botGenerationDetails)
        {
            var botBase = botGenerator.PrepareAndGenerateBot(mcsLeadPlayerId, botGenerationDetails);

            var playerName = randomUtil.GetArrayValue(_afdianNames);
            if (playerName is not null)
            {
                botBase.Info.Nickname = playerName;
                botBase.Info.LowerNickname = playerName.ToLower();
            }

            botBase.Info.Level = botGenerationDetails.PlayerLevel;
            botBase.Id = mcsBotPlayerId;
            botBase.SessionId = mcsBotPlayerId;
            botBase.Aid = hashUtil.GenerateAccountId();

            var pmcData = new PmcData
            {
                Id = botBase.Id,
                Aid = botBase.Aid,
                SessionId = botBase.SessionId,
                KarmaValue = botBase.KarmaValue,
                Info = botBase.Info,
                Customization = botBase.Customization,
                Health = botBase.Health,
                Inventory = botBase.Inventory,
                Skills = botBase.Skills,
                Stats = botBase.Stats,
                Encyclopedia = botBase.Encyclopedia,
                TaskConditionCounters = botBase.TaskConditionCounters,
                InsuredItems = botBase.InsuredItems,
                Hideout = botBase.Hideout,
                Quests = botBase.Quests,
                TradersInfo = botBase.TradersInfo,
                UnlockedInfo = botBase.UnlockedInfo,
                RagfairInfo = botBase.RagfairInfo,
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
                Prestige = new(),
            };
            return pmcData;
        }

        private PmcData GenerateScavData(PmcData pmcData, BotGenerationDetails botGenerationDetails)
        {
            var scavData = cloner.Clone(pmcData);
            scavData.Id = new();

            botGenerationDetails.IsPmc = false;
            botGenerationDetails.Side = "Savage";

            scavData.Info.Nickname = botNameService.GenerateUniqueBotNickname(
                cloner.Clone(botHelper.GetBotTemplate("assault")),
                botGenerationDetails,
                configServer.GetConfig<BotConfig>().BotRolesThatMustHaveUniqueName
            );

            return scavData;
        }

        private PmcData GenerateScavData(MongoId mcsLeadPlayerId, int carryServiceLevel, BotGenerationDetails botGenerationDetails, PmcData pmcData)
        {
            var scavKarmaLevel = Math.Clamp(carryServiceLevel + 2, -7, 6);
            var playerScavConfig = configServer.GetConfig<PlayerScavConfig>();

            if (!playerScavConfig.KarmaLevel.TryGetValue(scavKarmaLevel.ToString(CultureInfo.InvariantCulture), out var playerScavKarmaSettings))
            {
                logger.Error(serverLocalisationService.GetText("scav-missing_karma_settings", scavKarmaLevel));
            }

            botGenerationDetails.IsPmc = false;
            botGenerationDetails.Side = "Savage";
            botGenerationDetails.Role = playerScavKarmaSettings.BotTypeForLoot.ToLowerInvariant();

            var baseBotNode = cloner.Clone(botHelper.GetBotTemplate("assault"));

            var playerScavGeneratorTraverse = Traverse.Create(playerScavGenerator);
            playerScavGeneratorTraverse.Method("AdjustBotTemplateWithKarmaSpecificSettings", [playerScavKarmaSettings, baseBotNode]).GetValue();

            var botBase = botGenerator.PrepareAndGenerateBot(mcsLeadPlayerId, botGenerationDetails);

            var scavData = new PmcData
            {
                Id = new(),
                Aid = pmcData.Aid,
                SessionId = pmcData.SessionId,
                Savage = null,
                KarmaValue = botBase.KarmaValue,
                Info = botBase.Info,
                Customization = botBase.Customization,
                Health = botBase.Health,
                Inventory = botBase.Inventory,
                Skills = botBase.Skills,
                Stats = botBase.Stats,
                Encyclopedia = botBase.Encyclopedia,
                TaskConditionCounters = botBase.TaskConditionCounters,
                InsuredItems = botBase.InsuredItems,
                Hideout = botBase.Hideout,
                Quests = botBase.Quests,
                TradersInfo = pmcData.TradersInfo,
                UnlockedInfo = pmcData.UnlockedInfo,
                RagfairInfo = pmcData.RagfairInfo,
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
                Variables = new(),
                Prestige = new()
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

            return scavData;
        }

        private SptProfile GenerateFullProfile(PmcData pmcData, PmcData scavData)
        {
            pmcData.Savage = scavData.Id;
            scavData.SessionId = pmcData.SessionId;
            return new SptProfile
            {
                ProfileInfo = new Info
                {
                    ProfileId = pmcData.SessionId,
                    Username = pmcData.Info.Nickname,
                    Aid = pmcData.Aid,
                    ScavengerId = scavData.Id
                },
                CharacterData = new Characters
                {
                    PmcData = pmcData,
                    ScavData = scavData
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