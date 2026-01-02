using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class MCSProfileService(
        JsonUtil jsonUtil,
        FileUtil fileUtil,
        ISptLogger<MCSProfileService> logger,
        MCSConfigService mcsConfigService,
        RandomUtil randomUtil,
        DatabaseService databaseService,
        BotGenerator botGenerator
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

        public SptProfile? GetCSFullProfileByAccountId(MongoId sessionId, string csAccountId)
        {
            var check = int.TryParse(csAccountId, out var aid);
            if (!check)
            {
                logger.Error($"Account {csAccountId} does not exist");
            }

            _profiles.TryGetValue(sessionId, out var bossCSPlayerFullProfiles);
            if (bossCSPlayerFullProfiles is null)
            {
                return null;
            }
            return bossCSPlayerFullProfiles.FirstOrDefault(p => p.Value.ProfileInfo.Aid == aid).Value;
        }

        public async Task OnPostLoadAsync()
        {
            await LoadAllCSPlayerProfileAsync();
            await LoadAfdianPmcName();
        }
    }
}