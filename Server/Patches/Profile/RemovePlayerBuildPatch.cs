
using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
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
    public sealed class RemovePlayerBuildPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BuildController), nameof(BuildController.RemoveBuild));

        public RemovePlayerBuildPatch(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        private static IServiceProvider ServiceProvider;

        private static Controllers.ProfileController ProfileController { get => field ??= ServiceProvider.GetService<Controllers.ProfileController>(); }
        private static BuildsController BuildsController { get => field ??= ServiceProvider.GetService<BuildsController>(); }
        private static ProfileHelper ProfileHelper { get => field ??= ServiceProvider.GetService<ProfileHelper>(); }

        [PatchPostfix]
        public static void Postfix(MongoId sessionId, RemoveBuildRequestData request)
        {
            if (ProfileController.IsMcsBotPlayerInventoryMode(sessionId))
            {
                var profile = ProfileHelper.GetFullProfile(sessionId);
                _ = BuildsController.SaveUserBuilds(sessionId, profile.UserBuildData);
                var profiles = ProfileController.GetAllMcsBotPlayerProfileByBossId(sessionId);
                foreach (var _profile in profiles)
                {
                    if (_profile.ProfileInfo.ProfileId == profile.ProfileInfo.ProfileId)
                    {
                        continue;
                    }
                    _profile.UserBuildData = profile.UserBuildData;
                }
                _ = ProfileController.SaveAllMcsBotPlayerProfile(sessionId);
            }
        }
    }
}