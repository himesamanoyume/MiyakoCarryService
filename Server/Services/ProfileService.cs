using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Server.Generators.CustomGeneration;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Models.Mcs;
using MiyakoCarryService.Server.Utils;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Helpers.Bot;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Enums.RaidSettings;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Servers.Ws;
using SPTarkov.Server.Core.Services.Bot;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public class ProfileService(
        JsonUtil jsonUtil,
        FileUtil fileUtil,
        NotificationSendHelper notificationSendHelper,
        ISptLogger<ProfileService> logger,
        ConfigService configService,
        RandomUtil randomUtil,
        HashUtil hashUtil,
        ICloner cloner,
        GlobalTable globalTable,
        BotConfig botConfig,
        BotHelper botHelper,
        ProfileHelper profileHelper,
        BuildsService buildsService,
        InventoryHelper inventoryHelper,
        ServerLocalisationService serverLocalisationService,
        NotificationHelper notificationHelper,
        BotInventoryContainerService botInventoryContainerService,
        BotLootCacheService botLootCacheService,
        McsBotGenerator mcsBotGenerator,
        BotGenerator botGenerator,
        PlayerScavGenerator playerScavGenerator,
        SptWebSocketConnectionHandler sptWebSocketConnectionHandler,
        BotNameService botNameService,
        MailSendService mailSendService,
        InfoService infoService,
        TradersTable tradersTable,
        HideoutTable hideoutTable,
        PlayerScavConfig playerScavConfig,
        CompatibilityService compatibilityService
    )
    {
        private readonly string _profileFolderDir = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "profiles");
        private readonly string _ifdianFolderDir = System.IO.Path.Join(configService.GetModPath(), "Assets", "database", "bots", "types");

        private readonly ConcurrentDictionary<MongoId, ConcurrentDictionary<MongoId, SptProfile>> _profiles = new();
        private readonly ConcurrentDictionary<MongoId, int> _mcsInventoryModeIds = new();
        private readonly ConcurrentDictionary<MongoId, SemaphoreSlim> _saveLocks = new();
        private List<string> _ifdianNames = [];
        private Ifdian _ifdian;
        private SemaphoreSlim _saveLock = new(1, 1);

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
            }

            try
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    try
                    {
                        if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsLeadPlayerId))
                        {
                            var notification = notificationHelper.GenerateWsGroupMatchUserLeave(mcsBotPlayerProfile);
                            var notification2 = notificationHelper.GenerateWsFriendsListAccept(mcsBotPlayerProfile, NotificationEventType.youAreRemovedFromFriendList);
                            await notificationSendHelper.SendMessageAsync(mcsLeadPlayerId, notification);
                            await notificationSendHelper.SendMessageAsync(mcsLeadPlayerId, notification2);
                        }
                    }
                    finally
                    {

                    }
                });
            }
            finally
            {
                RemoveMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
            }
        }

        public void ProcessExpiredMcsBotPlayerProfiles(MongoId mcsLeadPlayerId, HashSet<MongoId> mcsBotPlayerIds)
        {
            foreach (var mcsBotPlayerId in mcsBotPlayerIds)
            {
                ProcessExpiredMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
            }
        }

        public void ProcessExpiredMcsBotPlayerNotify(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            var mcsBotPlayerProfile = GetMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
            if (mcsBotPlayerProfile is null)
            {
                var errorInfo = serverLocalisationService.GetText(Locales.FAILEDLOADMCSPLAYERPROFILE);
                logger.Error(errorInfo);
                return;
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                try
                {
                    if (sptWebSocketConnectionHandler.IsWebSocketConnected(mcsLeadPlayerId))
                    {
                        var notification = notificationHelper.GenerateWsFriendsListAccept(mcsBotPlayerProfile, NotificationEventType.friendListRequestAccept, true);
                        await notificationSendHelper.SendMessageAsync(mcsLeadPlayerId, notification);
                    }
                }
                finally
                {

                }
            });
        }

        public void ProcessExpiredMcsBotPlayerNotifies(MongoId mcsLeadPlayerId, HashSet<MongoId> mcsBotPlayerIds)
        {
            foreach (var mcsBotPlayerId in mcsBotPlayerIds)
            {
                ProcessExpiredMcsBotPlayerNotify(mcsLeadPlayerId, mcsBotPlayerId);
            }
        }

        public void TeamKillPunish(MongoId mcsLeadPlayerId)
        {
            infoService.SetAllOrderInfosToExpire(mcsLeadPlayerId, ProcessExpiredMcsBotPlayerNotify);
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

        public async Task<long> SaveAllMcsBotPlayerProfile(MongoId mcsLeadPlayerId)
        {
            var mcsBotPlayerProfiles = _profiles[mcsLeadPlayerId].Values;
            var start = Stopwatch.StartNew();
            foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
            {
                await SaveMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerProfile);
            }
            start.Stop();
            return start.ElapsedMilliseconds;
        }

        private async Task LoadMcsBotPlayerProfile(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
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

        private async Task LoadAllMcsBotPlayerProfile()
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
                            if (infoService.CheckMcsBotPlayerExist(mcsBotPlayerId))
                            {
                                await LoadMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
                            }
                        }
                    }
                }
            }
        }

        private async Task LoadIfdianPmcName()
        {
            if (!fileUtil.DirectoryExists(_ifdianFolderDir))
            {
                fileUtil.CreateDirectory(_ifdianFolderDir);
            }

            var ifdianFilePath = System.IO.Path.Join(_ifdianFolderDir, "ifdian.json");
            if (!fileUtil.FileExists(ifdianFilePath))
            {
                await fileUtil.WriteFileAsync(ifdianFilePath, jsonUtil.Serialize(new Ifdian
                {
                    Timestamp = 0,
                    Supporter = [
                        "姫様の夢",
                        "月雪ミヤコ"
                    ]
                }, true));
            }
            _ifdian = await jsonUtil.DeserializeFromFileAsync<Ifdian>(ifdianFilePath);
            _ifdianNames = _ifdian.Supporter;
        }

        public Ifdian GetIfdian()
        {
            return _ifdian;
        }

        public async Task SaveIfdian()
        {
            if (_saveLock is null)
            {
                _saveLock = new(1, 1);
            }

            await _saveLock.WaitAsync();
            try
            {
                try
                {
                    var ifdianString = jsonUtil.Serialize(_ifdian, true);
                    var ifdianFilePath = System.IO.Path.Join(_ifdianFolderDir, "ifdian.json");
                    await fileUtil.WriteFileAsync(ifdianFilePath, ifdianString);
                }
                catch
                {

                }
            }
            finally
            {
                _saveLock.Release();
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

        public void UpdateIfdianName()
        {
            _ifdianNames = _ifdian.Supporter;
        }

        public List<PmcData> GetMcsBotPlayerProfileForInventoryMode(MongoId mcsLeadPlayerId)
        {
            if (_mcsInventoryModeIds.TryGetValue(mcsLeadPlayerId, out var intMcsAid))
            {
                var mcsBotPlayerFullProfile = GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, intMcsAid);
                var mcsBotPlayerFullProfileClone = cloner.Clone(mcsBotPlayerFullProfile);

                var output = new List<PmcData>
                {
                    mcsBotPlayerFullProfileClone.CharacterData.PmcData,
                    mcsBotPlayerFullProfileClone.CharacterData.ScavData
                };

                return output;
            }

            return new();
        }

        public SptProfile? GetMcsBotPlayerFullProfileForInventoryMode(MongoId mcsLeadPlayerId)
        {
            if (_mcsInventoryModeIds.TryGetValue(mcsLeadPlayerId, out var intMcsAid))
            {
                return GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, intMcsAid);
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

        public async Task<bool> VerifyMcsBotPlayerAid(MongoId mcsLeadPlayerId, string mcsAid)
        {
            var isInt = int.TryParse(mcsAid, out var intMcsAid);
            if (!isInt)
            {
                logger.Error(string.Format(serverLocalisationService.GetText(Locales.ACCOUNTIDISINVAILD), mcsAid));
                return false;
            }

            if (_profiles.ContainsKey(mcsLeadPlayerId))
            {
                _profiles.TryGetValue(mcsLeadPlayerId, out var mcsBotPlayerProfiles);
                if (mcsBotPlayerProfiles is null)
                {
                    return false;
                }

                var verify = mcsBotPlayerProfiles.Any(p => p.Value.ProfileInfo.Aid == intMcsAid);
                if (verify)
                {
                    if (!_mcsInventoryModeIds.TryAdd(mcsLeadPlayerId, intMcsAid))
                    {
                        _mcsInventoryModeIds.TryRemove(mcsLeadPlayerId, out _);
                        _mcsInventoryModeIds.TryAdd(mcsLeadPlayerId, intMcsAid);
                    }
                }
                return verify;
            }
            return false;
        }

        public async Task<bool> RemoveMcsBotPlayerAid(MongoId mcsLeadPlayerId, string mcsAid)
        {
            var isInt = int.TryParse(mcsAid, out var intMcsAid);
            if (!isInt)
            {
                logger.Error(string.Format(serverLocalisationService.GetText(Locales.ACCOUNTIDISINVAILD), mcsAid));
                return false;
            }

            if (_profiles.ContainsKey(mcsLeadPlayerId))
            {
                _profiles.TryGetValue(mcsLeadPlayerId, out var mcsBotPlayerProfiles);
                if (mcsBotPlayerProfiles is null)
                {
                    return false;
                }

                var verify = _mcsInventoryModeIds.Any(p => p.Value == intMcsAid);
                if (verify)
                {
                    verify = _mcsInventoryModeIds.TryRemove(mcsLeadPlayerId, out _);
                }
                return verify;
            }
            return false;
        }

        public void RemoveMcsBotPlayerAid(MongoId mcsLeadPlayerId)
        {
            _mcsInventoryModeIds.TryRemove(mcsLeadPlayerId, out _);
        }

        public bool IsMcsBotPlayerInventoryMode(MongoId mcsLeadPlayerId)
        {
            return _mcsInventoryModeIds.ContainsKey(mcsLeadPlayerId);
        }

        private int GetRandomLevelByCarryServiceLevel(int carryServiceLevel)
        {
            return carryServiceLevel switch
            {
                1 => randomUtil.RandInt(1, 15),
                2 => randomUtil.RandInt(15, 30),
                3 => randomUtil.RandInt(30, 50),
                4 => randomUtil.RandInt(50, 70),
                >= 5 => randomUtil.RandInt(70, 79),
                _ => randomUtil.RandInt(1, 15)
            };
        }

        public SptProfile Generate(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId, PmcData completeQuestPmcData, OrderInfo orderInfo)
        {
            var isPmc = orderInfo.SpawnType.WildSpawnType is "common" or "pmcUSEC" or "pmcBEAR";
            var botDifficulty = (BotDifficulty)orderInfo.CarryServiceLevel;
            var role = isPmc ? (completeQuestPmcData.Info.Side == "Usec" ? "pmcUSEC" : "pmcBEAR") : orderInfo.SpawnType.WildSpawnType;
            var level = GetRandomLevelByCarryServiceLevel(orderInfo.CarryServiceLevel + (orderInfo.SpawnType.IsBoss ? 1 : 0));
            var botGenerationDetails = new BotGenerationDetails()
            {
                IsPmc = isPmc,
                Side = isPmc ? completeQuestPmcData.Info.Side : "Savage",
                Role = role,
                BotLevel = level,
                PlayerLevel = level,
                BotRelativeLevelDeltaMin = 0,
                BotRelativeLevelDeltaMax = 0,
                BotCountToGenerate = 1,
                BotDifficulty = botDifficulty <= BotDifficulty.Impossible && botDifficulty > BotDifficulty.AsOnline ? botDifficulty is BotDifficulty.Medium ? "normal" : botDifficulty.ToString().ToLower() : "impossible",
                IsPlayerScav = false,
                AllPmcsHaveSameNameAsPlayer = false
            };

            // 适配APBS重复添加Tier
            var clonedBotGenerationDetails = cloner.Clone(botGenerationDetails);

            PmcData pmcData;
            try
            {
                pmcData = GeneratePmcData(mcsLeadPlayerId, mcsBotPlayerId, botGenerationDetails, orderInfo);

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
                pmcData = GeneratePmcData(mcsLeadPlayerId, mcsBotPlayerId, botGenerationDetails, orderInfo);
            }
            pmcData.Info.Level = botGenerationDetails.PlayerLevel;

            PmcData scavData;
            try
            {
                scavData = isPmc ? GenerateScavData(mcsLeadPlayerId, clonedBotGenerationDetails, pmcData, orderInfo) : GenerateScavData(pmcData, clonedBotGenerationDetails);
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
                scavData = isPmc ? GenerateScavData(mcsLeadPlayerId, clonedBotGenerationDetails, pmcData, orderInfo) : GenerateScavData(pmcData, clonedBotGenerationDetails);
            }

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
            var userBuilds = buildsService.GetUserBuilds(mcsLeadPlayerId);
            if (userBuilds != null)
            {
                fullProfile.UserBuildData = userBuilds;
            }

            if (fullProfile.CharacterData.PmcData.Inventory.Items != null)
            {
                foreach (var item in fullProfile.CharacterData.PmcData.Inventory.Items)
                {
                    fullProfile.CharacterData.PmcData.Encyclopedia.TryAdd(item.Template, true);
                }
            }

            buildsService.ExaminedUserBuildsItem(fullProfile, fullProfile.UserBuildData);
            _ = SaveMcsBotPlayerProfile(mcsLeadPlayerId, fullProfile);
            return fullProfile;
        }

        private PmcData GeneratePmcData(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId, BotGenerationDetails botGenerationDetails, OrderInfo orderInfo)
        {
            var botBase = compatibilityService.HasAPBS ? botGenerator.PrepareAndGenerateBot(mcsLeadPlayerId, botGenerationDetails) : mcsBotGenerator.CustomPrepareAndGenerateBot(mcsLeadPlayerId, botGenerationDetails, orderInfo);

            var playerName = randomUtil.GetArrayValue(_ifdianNames);
            if (playerName is not null)
            {
                botBase.Info.Nickname = playerName;
                botBase.Info.LowerNickname = playerName.ToLower();
            }

            botBase.Info.Level = botGenerationDetails.PlayerLevel;
            botBase.Id = mcsBotPlayerId;
            botBase.SessionId = mcsBotPlayerId;
            botBase.Aid = hashUtil.GenerateAccountId();

            var tradersInfo = new Dictionary<MongoId, TraderInfo>();

            foreach (var (traderId, trader) in tradersTable)
            {
                tradersInfo[traderId] = new TraderInfo
                {
                    LoyaltyLevel = 4,
                    SalesSum = 999999999.0,
                    Standing = 10.0,
                    NextResupply = trader.Base?.NextResupply ?? 0,
                    Unlocked = true,
                    Disabled = false,
                };
            }

            var areas = new List<BotHideoutArea>();

            var hideoutAreas = hideoutTable.Areas;

            foreach (HideoutAreas areaType in Enum.GetValues(typeof(HideoutAreas)))
            {
                if (areaType == HideoutAreas.NotSet)
                {
                    continue;
                }

                var dbArea = hideoutAreas.FirstOrDefault(area => area.Type == areaType);
                int maxLevel = 0;
                MongoId? containerId = null;

                if (dbArea != null && dbArea.Stages != null)
                {
                    maxLevel = dbArea.Stages.Count - 1; // 等级从0开始，所以减1  

                    if (maxLevel >= 0 && dbArea.Stages.TryGetValue(maxLevel.ToString(), out var maxStage))
                    {
                        if (maxStage.Container.HasValue && !maxStage.Container.Value.IsEmpty)
                        {
                            containerId = maxStage.Container.Value;
                        }
                    }
                }

                areas.Add(new BotHideoutArea
                {
                    Type = areaType,
                    Level = maxLevel,
                    Active = true,
                    PassiveBonusesEnabled = areaType != HideoutAreas.ChristmasIllumination,
                    CompleteTime = 0,
                    Constructing = false,
                    Slots = new(),
                    LastRecipe = "",
                });
            }

            var random = new Random();
            var bytes = new byte[16];
            random.NextBytes(bytes);
            var hideoutSeed = BitConverter.ToString(bytes).Replace("-", "").ToLower();

            var hideout = new Hideout
            {
                Areas = areas,
                Production = new(),
                Improvements = new(),
                Seed = hideoutSeed,
                Customization = new()
                {
                    { "Wall", "675844bdf94a97cbbe096f1a" },
                    { "Floor", "6758443ff94a97cbbe096f18" },
                    { "Light", "675fe8abbc3deae49a0b947f" },
                    { "Ceiling", "673b3f977038192ee006aa09" },
                    { "ShootingRangeMark", "67585d416c72998cf60ed85a" },
                },
                MannequinPoses = new(),
            };

            var moneyTransferLimitData = new MoneyTransferLimits
            {
                NextResetTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 86400,
                RemainingLimit = 1000000,
                TotalLimit = 1000000,
                ResetInterval = 86400,
            };

            var expTable = globalTable.Configuration.Exp.Level.ExperienceTable;
            botBase.Info.Experience = expTable.Take(botGenerationDetails.PlayerLevel.Value).Sum(entry => entry.Experience);

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
                Encyclopedia = new(),
                TaskConditionCounters = botBase.TaskConditionCounters,
                InsuredItems = botBase.InsuredItems,
                Hideout = hideout,
                Quests = new(),
                TradersInfo = tradersInfo,
                UnlockedInfo = botBase.UnlockedInfo,
                RagfairInfo = botBase.RagfairInfo,
                Achievements = new(),
                RepeatableQuests = new(),
                Bonuses = botBase.Bonuses,
                Notes = new(),
                CarExtractCounts = botBase.CarExtractCounts,
                CoopExtractCounts = botBase.CoopExtractCounts,
                SurvivorClass = botBase.SurvivorClass,
                WishList = botBase.WishList,
                MoneyTransferLimitData = moneyTransferLimitData,
                IsPmc = botBase.IsPmc,
                Prestige = new(),
            };

            var currencies = new Dictionary<MongoId, int>
            {
                { Money.EUROS, 114514 },
                { Money.DOLLARS, 1919810 },
                { Money.GP, 1314 },
                { Money.ROUBLES, 151247016 },
            };

            var stashId = pmcData.Inventory.Stash.Value;
            var inventoryHelperTraverse = Traverse.Create(inventoryHelper);

            foreach (var (moneyId, amount) in currencies)
            {
                var item = new Item
                {
                    Id = new(),
                    Template = moneyId,
                    Upd = new Upd
                    {
                        StackObjectsCount = amount
                    }
                };

                var stashFS2D = inventoryHelperTraverse.Method("GetStashSlotMap", [pmcData]).GetValue<int[,]>();
                var placeResult = inventoryHelper.PlaceItemInContainer(
                    stashFS2D,
                    [item],
                    stashId,
                    "hideout"
                );

                if (!placeResult.Success.GetValueOrDefault(false))
                {
                    continue;
                }

                pmcData.Inventory.Items.Add(item);
            }

            var mcsFont = new Dictionary<char, string[]>
            {
                ['M'] = ["10001", "11011", "10101", "10001", "10001"],
                ['Y'] = ["10001", "01010", "00100", "00100", "00100"],
                ['L'] = ["10000", "10000", "10000", "10000", "11111"],
                ['O'] = ["01110", "10001", "10001", "10001", "01110"],
                ['V'] = ["10001", "10001", "10001", "01010", "00100"],
                ['E'] = ["11111", "10000", "11110", "10000", "11111"],
                ['I'] = ["11111", "00100", "00100", "00100", "11111"],
                ['A'] = ["01110", "10001", "11111", "10001", "10001"],
                ['K'] = ["10001", "10010", "11100", "10010", "10001"],
            };
            var mcsAmounts = new[] { 143, 214, 520, 521, 5963, 4869, 236, 1, 2945, 10107, 15, 156 };

            const string mcsText = "MYLOVEMIYAKO";
            const int startX = 2;
            const int letterGap = 1;
            int cursorY = 1;

            for (int i = 0; i < mcsText.Length; i++)
            {
                var ch = mcsText[i];
                if (!mcsFont.TryGetValue(ch, out var glyph))
                {
                    continue;
                }

                var amount = mcsAmounts[i]; // 当前字母使用的卢布数量  

                for (int row = 0; row < glyph.Length; row++)
                {
                    var line = glyph[row];
                    for (int col = 0; col < line.Length; col++)
                    {
                        if (line[col] != '1')
                        {
                            continue;
                        }

                        int x = startX + col;
                        int y = cursorY + row;
                        if (x < 0 || x > 9 || y < 0 || y > 71)
                        {
                            continue; // 越界保护  
                        }

                        var pixel = new Item
                        {
                            Id = new(),
                            Template = Money.ROUBLES,
                            ParentId = stashId,
                            SlotId = "hideout",
                            Location = new ItemLocation { X = x, Y = y, R = ItemRotation.Horizontal },
                            Upd = new Upd { StackObjectsCount = amount }
                        };
                        pmcData.Inventory.Items.Add(pixel);
                    }
                }

                cursorY += glyph.Length + letterGap;
            }

            if (pmcData.Inventory.HideoutAreaStashes == null)
            {
                pmcData.Inventory.HideoutAreaStashes = new Dictionary<string, MongoId>();
            }

            foreach (var dbArea in hideoutAreas)
            {
                if (dbArea.Type == null || dbArea.Stages == null)
                {
                    continue;
                }

                var profileArea = areas.FirstOrDefault(area => area.Type == dbArea.Type);
                if (profileArea == null || profileArea.Level < 0)
                {
                    continue;
                }

                if (!dbArea.Stages.TryGetValue(profileArea.Level.ToString(), out var stage))
                {
                    continue;
                }

                if (!stage.Container.HasValue || stage.Container.Value.IsEmpty)
                {
                    continue;
                }

                var keyForHideoutAreaStash = ((int)dbArea.Type).ToString();
                if (!pmcData.Inventory.HideoutAreaStashes.ContainsKey(keyForHideoutAreaStash))
                {
                    pmcData.Inventory.HideoutAreaStashes[keyForHideoutAreaStash] = dbArea.Id;
                }

                var existingInventoryItem = pmcData.Inventory.Items.FirstOrDefault(item => item.Id == dbArea.Id);
                if (existingInventoryItem == null)
                {
                    var newContainerItem = new Item
                    {
                        Id = dbArea.Id,
                        Template = stage.Container.Value
                    };
                    pmcData.Inventory.Items.Add(newContainerItem);
                }
                else
                {
                    existingInventoryItem.Template = stage.Container.Value;
                }
            }

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
                botConfig.BotRolesThatMustHaveUniqueName
            );

            return scavData;
        }

        private PmcData GenerateScavData(MongoId mcsLeadPlayerId, BotGenerationDetails botGenerationDetails, PmcData pmcData, OrderInfo orderInfo)
        {
            var scavKarmaLevel = Math.Clamp(orderInfo.CarryServiceLevel + 2, -7, 6);
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

            var botBase = compatibilityService.HasAPBS ? botGenerator.PrepareAndGenerateBot(mcsLeadPlayerId, botGenerationDetails) : mcsBotGenerator.CustomPrepareAndGenerateBot(mcsLeadPlayerId, botGenerationDetails, orderInfo);

            var expTable = globalTable.Configuration.Exp.Level.ExperienceTable;
            botBase.Info.Experience = expTable.Take(botGenerationDetails.PlayerLevel.Value).Sum(entry => entry.Experience);

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
                Encyclopedia = pmcData.Encyclopedia,
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
                ProfileInfo = new SPTarkov.Server.Core.Models.Eft.Profile.Info
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
                },
                UserBuildData = new UserBuilds
                {
                    EquipmentBuilds = [],
                    WeaponBuilds = [],
                    MagazineBuilds = [],
                },
                DialogueRecords = new(),
                SptData = profileHelper.GetDefaultSptDataObject(),
                InraidData = new(),
                InsuranceList = [],
                BtrDeliveryList = [],
                TraderPurchases = [],
                FriendProfileIds = [],
                CustomisationUnlocks = [],
            };
        }

        public bool SettleOrder(MongoId mcsLeadPlayerId, string aid)
        {
            if (IsMcsBotPlayerInventoryMode(mcsLeadPlayerId))
            {
                return false;
            }
            var profile = GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, aid);
            if (profile is null)
            {
                return false;
            }
            var botProfileId = profile.ProfileInfo.ProfileId.Value;
            var playerIds = infoService.SettleOrderByBotPlayerProfileId(botProfileId);
            if (playerIds is null)
            {
                return false;
            }
            ProcessExpiredMcsBotPlayerProfiles(mcsLeadPlayerId, playerIds);
            return true;
        }

        public async Task OnPostLoadAsync()
        {
            await LoadAllMcsBotPlayerProfile();
            await LoadIfdianPmcName();
        }
    }
}