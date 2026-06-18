
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class BuildsService(
        ConfigService configService,
        JsonUtil jsonUtil,
        FileUtil fileUtil
    )
    {
        private readonly string _userbuildsFolderDir = Path.Join(configService.GetModPath(), "Assets", "database", "bots", "userbuilds");
        private readonly ConcurrentDictionary<MongoId, UserBuilds> _userBuilds = new();
        private readonly ConcurrentDictionary<MongoId, SemaphoreSlim> _saveLocks = new();

        public async Task OnPostLoadAsync()
        {
            await LoadAllUserBuilds();
        }

        private async Task LoadAllUserBuilds()
        {
            if (!fileUtil.DirectoryExists(_userbuildsFolderDir))
            {
                fileUtil.CreateDirectory(_userbuildsFolderDir);
            }

            var files = fileUtil.GetFiles(_userbuildsFolderDir).Where(item => fileUtil.GetFileExtension(item) == "json");
            foreach (var userBuildsPath in files)
            {
                var mcsLeadPlayerId = System.IO.Path.GetFileNameWithoutExtension(userBuildsPath);
                if (MongoId.IsValidMongoId(mcsLeadPlayerId))
                {
                    await LoadUserBuilds(mcsLeadPlayerId, userBuildsPath);
                }
            }
        }

        private async Task LoadUserBuilds(MongoId mcsLeadPlayerId, string userBuildsPath)
        {
            if (!fileUtil.FileExists(userBuildsPath))
            {
                await fileUtil.WriteFileAsync(userBuildsPath, jsonUtil.Serialize(new UserBuilds
                {
                    EquipmentBuilds = [],
                    WeaponBuilds = [],
                    MagazineBuilds = [],
                }, true));
            }

            var userBuilds = await jsonUtil.DeserializeFromFileAsync<UserBuilds>(userBuildsPath);
            if (userBuilds is not null)
            {
                _userBuilds.GetOrAdd(mcsLeadPlayerId, _ => userBuilds);
            }
        }

        public async Task SaveUserBuilds(MongoId mcsLeadPlayerId, UserBuilds userBuilds)
        {
            var saveLock = _saveLocks.GetOrAdd(mcsLeadPlayerId, _ => new(1, 1));
            await saveLock.WaitAsync();
            try
            {
                try
                {
                    var userBuildsPath = System.IO.Path.Combine(_userbuildsFolderDir, $"{mcsLeadPlayerId}.json");
                    _userBuilds.GetOrAdd(mcsLeadPlayerId, userBuilds);
                    var jsonUserBuilds = jsonUtil.Serialize(_userBuilds[mcsLeadPlayerId], true);
                    await fileUtil.WriteFileAsync(userBuildsPath, jsonUserBuilds);
                }
                catch (System.Exception)
                {
                    
                }
            }
            finally
            {
                saveLock.Release();
            }
        }

        public UserBuilds? GetUserBuilds(MongoId mcsLeadPlayerId)
        {
            if (_userBuilds.ContainsKey(mcsLeadPlayerId))
            {
                _userBuilds.TryGetValue(mcsLeadPlayerId, out var userBuilds);
                if (userBuilds is null)
                {
                    return null;
                }
                return userBuilds;
            }
            return null;
        }

        public void ExaminedUserBuildsItem(SptProfile fullProfile, UserBuilds? userBuilds)
        {
            if (userBuilds == null)
            {
                return;
            }

            foreach (var weaponBuild in fullProfile.UserBuildData.WeaponBuilds)
            {
                foreach (var item in weaponBuild.Items)
                {
                    fullProfile.CharacterData.PmcData.Encyclopedia.TryAdd(item.Template, true);
                }
            }

            foreach (var equipmentBuild in fullProfile.UserBuildData.EquipmentBuilds)
            {
                foreach (var item in equipmentBuild.Items)
                {
                    fullProfile.CharacterData.PmcData.Encyclopedia.TryAdd(item.Template, true);
                }
            }

            foreach (var magazineBuild in fullProfile.UserBuildData.MagazineBuilds)
            {
                foreach (var item in magazineBuild.Items)
                {
                    fullProfile.CharacterData.PmcData.Encyclopedia.TryAdd(item.TemplateId, true);
                }
            }
        }
    }
}
