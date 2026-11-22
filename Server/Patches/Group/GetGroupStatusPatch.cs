
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Models.Eft.Match;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Patches.Group
{
    /// <summary>
    /// 使玩家进入匹配界面时能够自动加载其他队友的模型
    /// </summary>
    [Injectable]
    public sealed class GetGroupStatusPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchCallbacks), nameof(MatchCallbacks.GetGroupStatus));

        public GetGroupStatusPatch(
            RaidController raidController,
            NotificationHelper notificationHelper,
            NotificationSendHelper notificationSendHelper,
            HttpResponseUtil httpResponseUtil)
        {
            _raidController = raidController;
            _notificationHelper = notificationHelper;
            _notificationSendHelper = notificationSendHelper;
            _httpResponseUtil = httpResponseUtil;
        }

        private static RaidController _raidController;
        private static NotificationHelper _notificationHelper;
        private static NotificationSendHelper _notificationSendHelper;
        private static HttpResponseUtil _httpResponseUtil;

        [PatchPostfix]
        public static void Postfix(string url, MatchGroupStatusRequest info, MongoId sessionID, ref ValueTask<string> __result)
        {
            var mcsBotPlayerProfiles = _raidController.GetAllGroupMemberProfiles(sessionID);
            var isScav = info.IsSavage ?? false;

            if (mcsBotPlayerProfiles.Count > 0)
            {
                var players = new List<McsGroupCharacter>();
                foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
                {
                    players.Add(_notificationHelper.GenerateGroupCharacter(mcsBotPlayerProfile, isScav));
                }

                __result = new ValueTask<string>(_httpResponseUtil.GetBody(new McsMatchGroupStatusResponse
                {
                    Players = players,
                    MaxPveCountExceeded = false
                }));
            }

            foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
            {
                try
                {
                    var notification = _notificationHelper.GenerateWsGroupMatchRaidReady(mcsBotPlayerProfile, isScav);
                    _ = _notificationSendHelper.SendMessageAsync(sessionID, notification);
                }
                finally
                {

                }
            }
        }
    }
}