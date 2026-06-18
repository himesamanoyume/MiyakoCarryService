
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;

namespace MiyakoCarryService.Server.Patches.Profile
{
    /// <summary>
    /// 检测到玩家处于护航库存模式时，改为获取此玩家的护航预设
    /// </summary>
    public sealed class SaveWeaponBuildPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BuildController), nameof(BuildController.SaveWeaponBuild));

        [PatchPostfix]
        public static void Postfix(MongoId sessionId, PresetBuildActionRequestData request)
        {
            var profileController = ServiceLocator.ServiceProvider.GetService<Controllers.ProfileController>();

            if (profileController.IsMcsBotPlayerInventoryMode(sessionId))
            {
                var buildsController = ServiceLocator.ServiceProvider.GetService<BuildsController>();
                var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();
                var profile = profileHelper.GetFullProfile(sessionId);
                _ = buildsController.SaveUserBuilds(sessionId, profile.UserBuildData);
                var profiles = profileController.GetAllMcsBotPlayerProfileByBossId(sessionId);
                foreach (var _profile in profiles)
                {
                    if (_profile.ProfileInfo.ProfileId == profile.ProfileInfo.ProfileId)
                    {
                        continue;
                    }
                    _profile.UserBuildData = profile.UserBuildData;
                    buildsController.ExaminedUserBuildsItem(_profile, _profile.UserBuildData);
                }
                _ = profileController.SaveAllMcsBotPlayerProfile(sessionId);
            }
        }
    }
}