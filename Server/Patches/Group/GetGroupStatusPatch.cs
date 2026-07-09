
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Helper;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
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

        private static RaidController RaidController { get => field ??= ServiceLocator.ServiceProvider.GetService<RaidController>(); }
        private static NotificationHelper NotificationHelper { get => field ??= ServiceLocator.ServiceProvider.GetService<NotificationHelper>(); }
        private static NotificationSendHelper NotificationSendHelper { get => field ??= ServiceLocator.ServiceProvider.GetService<NotificationSendHelper>(); }

        [PatchPostfix]
        public static void Postfix(string url, MatchGroupStatusRequest info, MongoId sessionID)
        {
            var mcsBotPlayerProfiles = RaidController.GetAllGroupMemberProfiles(sessionID);
            foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
            {
                try
                {
                    var notification = NotificationHelper.GenerateWsGroupMatchRaidReady(mcsBotPlayerProfile, info.IsSavage.Value);
                    NotificationSendHelper.SendMessage(sessionID, notification);
                }
                finally
                {

                }
            }
        }
    }
}