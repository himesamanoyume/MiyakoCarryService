
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;

namespace MiyakoCarryService.Server.Patches.Profile
{
    /// <summary>
    /// 检测到玩家处于护航库存模式时，改为获取此玩家的护航预设
    /// </summary>
    [Injectable]
    public sealed class RemovePlayerBuildPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BuildController), nameof(BuildController.RemoveBuild));

        public RemovePlayerBuildPatch(Controllers.ProfileController profileController, BuildsController buildsController, ProfileHelper profileHelper)
        {
            _profileController = profileController;
            _buildsController = buildsController;
            _profileHelper = profileHelper;
        }

        private static Controllers.ProfileController _profileController;
        private static BuildsController _buildsController;
        private static ProfileHelper _profileHelper;

        [PatchPostfix]
        public static void Postfix(MongoId sessionId, RemoveBuildRequestData request)
        {
            if (_profileController.IsMcsBotPlayerInventoryMode(sessionId))
            {
                var profile = _profileHelper.GetFullProfile(sessionId);
                _ = _buildsController.SaveUserBuilds(sessionId, profile.UserBuildData);
                var profiles = _profileController.GetAllMcsBotPlayerProfileByBossId(sessionId);
                foreach (var _profile in profiles)
                {
                    if (_profile.ProfileInfo.ProfileId == profile.ProfileInfo.ProfileId)
                    {
                        continue;
                    }
                    _profile.UserBuildData = profile.UserBuildData;
                }
                _ = _profileController.SaveAllMcsBotPlayerProfile(sessionId);
            }
        }
    }
}