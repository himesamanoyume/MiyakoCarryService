
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Models.Eft.Match;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Patches.Group
{
    /// <summary>
    /// 向护航发送组队邀请时自动接受
    /// </summary>
    [Injectable]
    public sealed class SendGroupInvitePatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SPTarkov.Server.Core.Callbacks.MatchCallbacks), nameof(SPTarkov.Server.Core.Callbacks.MatchCallbacks.SendGroupInvite));

        public SendGroupInvitePatch(
            RaidController raidController,
            ProfileService profileService,
            NotificationHelper notificationHelper,
            HttpResponseUtil httpResponseUtil)
        {
            _raidController = raidController;
            _profileService = profileService;
            _notificationHelper = notificationHelper;
            _httpResponseUtil = httpResponseUtil;
        }

        private static RaidController _raidController;
        private static ProfileService _profileService;
        private static NotificationHelper _notificationHelper;
        private static HttpResponseUtil _httpResponseUtil;

        [PatchPrefix]
        public static void Prefix(string url, MatchGroupInviteSendRequest info, MongoId sessionID)
        {
            var isInt = int.TryParse(info.To, out var mcsAid);
            if (!isInt)
            {
                return;
            }
            _raidController.AcceptGroupInvite(sessionID, mcsAid);
        }

        /// <summary>
        /// 修正邀请响应。
        /// </summary>
        /// </remarks>
        [PatchPostfix]
        public static void Postfix(MatchGroupInviteSendRequest info, MongoId sessionID, ref ValueTask<string> __result)
        {
            McsGroupCharacter groupPlayer = null;

            if (int.TryParse(info.To, out var mcsAid))
            {
                var mcsBotPlayerProfile = _profileService.GetMcsBotPlayerProfileByAccountId(sessionID, mcsAid);
                if (mcsBotPlayerProfile is not null)
                {
                    groupPlayer = _notificationHelper.GenerateGroupCharacter(mcsBotPlayerProfile, false);
                }
            }

            __result = new ValueTask<string>(_httpResponseUtil.GetBody(new McsGroupInviteResponse
            {
                RequestId = new MongoId().ToString(),
                GroupPlayer = groupPlayer
            }));
        }
    }

    /// <summary>
    /// 玩家离队时清空护航成员
    /// </summary>
    [Injectable]
    public sealed class LeaveGroupPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SPTarkov.Server.Core.Callbacks.MatchCallbacks), nameof(SPTarkov.Server.Core.Callbacks.MatchCallbacks.LeaveGroup));

        public LeaveGroupPatch(RaidController raidController)
        {
            _raidController = raidController;
        }

        private static RaidController _raidController;

        [PatchPrefix]
        public static void Prefix(string url, EmptyRequestData _, MongoId sessionID)
        {
            _raidController.ClearGroupMember(sessionID);
        }
    }
}