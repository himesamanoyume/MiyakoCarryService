
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

        [PatchPostfix]
        public static void Postfix(string url, MatchGroupStatusRequest info, MongoId sessionID)
        {
            var raidController = ServiceLocator.ServiceProvider.GetService<RaidController>();
            var notificationHelper = ServiceLocator.ServiceProvider.GetService<NotificationHelper>();
            var notificationSendHelper = ServiceLocator.ServiceProvider.GetService<NotificationSendHelper>();
            var mcsBotPlayerProfiles = raidController.GetAllGroupMemberProfiles(sessionID);
            foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
            {
                try
                {
                    var notification = notificationHelper.GenerateWsGroupMatchRaidReady(mcsBotPlayerProfile, info.IsSavage.Value);
                    notificationSendHelper.SendMessage(sessionID, notification);
                }
                finally
                {

                }
            }
        }
    }
}