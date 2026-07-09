
using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;

namespace MiyakoCarryService.Server.Patches.Profile
{
    /// <summary>
    /// 检测到玩家处于护航库存模式时，改为获取此玩家的护航预设
    /// </summary>
    public sealed class SaveEquipmentBuildPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BuildController), nameof(BuildController.SaveEquipmentBuild));

        public SaveEquipmentBuildPatch(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        private static IServiceProvider ServiceProvider;

        private static Controllers.ProfileController ProfileController { get => field ??= ServiceProvider.GetService<Controllers.ProfileController>(); }
        private static BuildsController BuildsController { get => field ??= ServiceProvider.GetService<BuildsController>(); }
        private static ProfileHelper ProfileHelper { get => field ??= ServiceProvider.GetService<ProfileHelper>(); }

        [PatchPostfix]
        public static void Postfix(MongoId sessionID, PresetBuildActionRequestData request)
        {
            if (ProfileController.IsMcsBotPlayerInventoryMode(sessionID))
            {
                var profile = ProfileHelper.GetFullProfile(sessionID);
                _ = BuildsController.SaveUserBuilds(sessionID, profile.UserBuildData);
                var profiles = ProfileController.GetAllMcsBotPlayerProfileByBossId(sessionID);
                foreach (var _profile in profiles)
                {
                    if (_profile.ProfileInfo.ProfileId == profile.ProfileInfo.ProfileId)
                    {
                        continue;
                    }
                    _profile.UserBuildData = profile.UserBuildData;
                    BuildsController.ExaminedUserBuildsItem(_profile, _profile.UserBuildData);
                }
                _ = ProfileController.SaveAllMcsBotPlayerProfile(sessionID);
            }
        }
    }
}