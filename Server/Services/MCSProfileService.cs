using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
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
        ProfileHelper profileHelper,
        RandomUtil randomUtil,
        DatabaseService databaseService,
        BotGenerator botGenerator
    )
    {
        private readonly string _profileFolderDir = System.IO.Path.Join(mcsConfigService.GetModPath(), "Assets", "database", "profiles");
        private readonly string _afdianFolderDir = System.IO.Path.Join(mcsConfigService.GetModPath(), "Assets", "database", "bots", "types");

        private readonly ConcurrentDictionary<MongoId, ConcurrentDictionary<MongoId, BotBase>> _profiles = new();
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

        public void SaveMCPlayerProfile(MongoId sessionId, BotBase profile)
        {
            var csPlayerSessionId = (MongoId)profile.Id;
            var profilePath = System.IO.Path.Combine(_profileFolderDir, sessionId, $"{csPlayerSessionId}.json");
            _profiles[sessionId][csPlayerSessionId] = profile;
            var jsonProfile = jsonUtil.Serialize(_profiles[sessionId][csPlayerSessionId], false);
            fileUtil.WriteFile(profilePath, jsonProfile);
        }

        public async Task LoadCSPlayerProfileAsync(MongoId sessionId, MongoId csPlayeSessionId)
        {
            var filePath = System.IO.Path.Combine(_profileFolderDir, sessionId, $"{csPlayeSessionId}.json");
            if (fileUtil.FileExists(filePath))
            {
                var profile = await jsonUtil.DeserializeFromFileAsync<BotBase>(filePath);
                if (profile is not null)
                {
                    _profiles[sessionId][csPlayeSessionId] = profile;
                }
            }
        }

        private async Task LoadAllCSPlayerProfileAsync()
        {
            if (!fileUtil.DirectoryExists(_profileFolderDir))
            {
                fileUtil.CreateDirectory(_profileFolderDir);
            }

            var sessionIds = fileUtil.GetDirectories(_profileFolderDir);
            foreach (var sessionId in sessionIds)
            {
                var csPlayerProfileFolderPath = System.IO.Path.Combine(_profileFolderDir, sessionId);
                var files = fileUtil.GetFiles(csPlayerProfileFolderPath).Where(item => fileUtil.GetFileExtension(item) == "json");
                foreach (var file in files)
                {
                    var filename = System.IO.Path.GetFileNameWithoutExtension(file);
                    if (MongoId.IsValidMongoId(filename))
                    {
                        await LoadCSPlayerProfileAsync(sessionId, fileUtil.StripExtension(file));
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

        public BotBase GenerateBotProfile(MongoId sessionId, int carryServiceLevel)
        {
            var pmcProfile = profileHelper.GetPmcProfile(sessionId);
            var playerName = randomUtil.GetArrayValue(_afdianNames);
            var bots = databaseService.GetBots().Types;
            var pmcNames = new List<string>();
            pmcNames.AddRange(bots["usec"].FirstNames);
            pmcNames.AddRange(bots["bear"].FirstNames);
            
            var botBase = botGenerator.PrepareAndGenerateBot(sessionId, new()
            {
                IsPmc = true,
                
                Side = pmcProfile.Info.Side,
                Role = pmcProfile.Info.Side == "Usec" ? "pmcUSEC" : "pmcBEAR",
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

        public async Task OnPostLoadAsync()
        {
            await LoadAllCSPlayerProfileAsync();
            await LoadAfdianPmcName();
        }
    }
}