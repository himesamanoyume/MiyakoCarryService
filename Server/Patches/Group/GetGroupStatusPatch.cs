
using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Helper;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;

namespace MiyakoCarryService.Server.Patches.Group
{
    /// <summary>
    /// 使玩家进入匹配界面时能够自动加载其他队友的模型
    /// </summary>
    public sealed class GetGroupStatusPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchCallbacks), nameof(MatchCallbacks.GetGroupStatus));

        public GetGroupStatusPatch(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        private static IServiceProvider ServiceProvider;

        private static RaidController RaidController { get => field ??= ServiceProvider.GetService<RaidController>(); }
        private static NotificationHelper NotificationHelper { get => field ??= ServiceProvider.GetService<NotificationHelper>(); }
        private static NotificationSendHelper NotificationSendHelper { get => field ??= ServiceProvider.GetService<NotificationSendHelper>(); }

        [PatchPostfix]
        public static void Postfix(string url, MatchGroupStatusRequest info, MongoId sessionID)
        {
            var mcsBotPlayerProfiles = RaidController.GetAllGroupMemberProfiles(sessionID);
            foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
            {
                try
                {
                    var notification = NotificationHelper.GenerateWsGroupMatchRaidReady(mcsBotPlayerProfile, info.IsSavage.Value);
                    _ = NotificationSendHelper.SendMessageAsync(sessionID, notification);
                }
                finally
                {

                }
            }
        }
    }
}