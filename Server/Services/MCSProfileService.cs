using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
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
        ProfileValidatorService profileValidatorService,
        ISptLogger<MCSProfileService> logger,
        MCSConfigService mcsConfigService
    )
    {
        private readonly string _profileFolderDir = System.IO.Path.Join(mcsConfigService.GetModPath(), "Assets", "database", "profiles");

        private readonly ConcurrentDictionary<MongoId, ConcurrentDictionary<MongoId, SptProfile>> _profiles = new();

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

        public void SaveMCPlayerProfile(MongoId sessionId, SptProfile profile)
        {
            var csPlayerSessionId = (MongoId)profile.ProfileInfo.ProfileId;
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
                var profile = await jsonUtil.DeserializeFromFileAsync<JsonObject>(filePath);
                if (profile is not null)
                {
                    try
                    {
                        _profiles[sessionId][csPlayeSessionId] = profileValidatorService.MigrateAndValidateProfile(profile);
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.Critical($"Failed to load profile with ID '{sessionId}'");
                        logger.Critical(ex.ToString());
                    }
                }
            }
        }

        public async Task LoadAllCSPlayerProfileAsync()
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

        public async Task OnPostLoadAsync()
        {
            await LoadAllCSPlayerProfileAsync();
        }
    }
}