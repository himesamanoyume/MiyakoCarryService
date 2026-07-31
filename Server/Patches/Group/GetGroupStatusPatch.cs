
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Helper;
using SPTarkov.DI.Annotations;
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
    [Injectable]
    public sealed class GetGroupStatusPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchCallbacks), nameof(MatchCallbacks.GetGroupStatus));

        public GetGroupStatusPatch(RaidController raidController, NotificationHelper notificationHelper, NotificationSendHelper notificationSendHelper)
        {
            _raidController = raidController;
            _notificationHelper = notificationHelper;
            _notificationSendHelper = notificationSendHelper;
        }

        private static RaidController _raidController;
        private static NotificationHelper _notificationHelper;
        private static NotificationSendHelper _notificationSendHelper;

        [PatchPostfix]
        public static void Postfix(string url, MatchGroupStatusRequest info, MongoId sessionID)
        {
            var mcsBotPlayerProfiles = _raidController.GetAllGroupMemberProfiles(sessionID);
            foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
            {
                try
                {
                    var notification = _notificationHelper.GenerateWsGroupMatchRaidReady(mcsBotPlayerProfile, info.IsSavage.Value);
                    _ = _notificationSendHelper.SendMessageAsync(sessionID, notification);
                }
                finally
                {

                }
            }
        }
    }
}