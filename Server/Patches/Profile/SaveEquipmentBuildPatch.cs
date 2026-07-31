
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
    public sealed class SaveEquipmentBuildPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BuildController), nameof(BuildController.SaveEquipmentBuild));

        public SaveEquipmentBuildPatch(Controllers.ProfileController profileController, BuildsController buildsController, ProfileHelper profileHelper)
        {
            _profileController = profileController;
            _buildsController = buildsController;
            _profileHelper = profileHelper;
        }

        private static Controllers.ProfileController _profileController;
        private static BuildsController _buildsController;
        private static ProfileHelper _profileHelper;

        [PatchPostfix]
        public static void Postfix(MongoId sessionID, PresetBuildActionRequestData request)
        {
            if (_profileController.IsMcsBotPlayerInventoryMode(sessionID))
            {
                var profile = _profileHelper.GetFullProfile(sessionID);
                _ = _buildsController.SaveUserBuilds(sessionID, profile.UserBuildData);
                var profiles = _profileController.GetAllMcsBotPlayerProfileByBossId(sessionID);
                foreach (var _profile in profiles)
                {
                    if (_profile.ProfileInfo.ProfileId == profile.ProfileInfo.ProfileId)
                    {
                        continue;
                    }
                    _profile.UserBuildData = profile.UserBuildData;
                    _buildsController.ExaminedUserBuildsItem(_profile, _profile.UserBuildData);
                }
                _ = _profileController.SaveAllMcsBotPlayerProfile(sessionID);
            }
        }
    }
}