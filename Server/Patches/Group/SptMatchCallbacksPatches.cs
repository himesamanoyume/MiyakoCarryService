
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Match;

namespace MiyakoCarryService.Server.Patches.Group
{
    /// <summary>
    /// 向护航发送组队邀请时自动接受
    /// </summary>
    [Injectable]
    public sealed class SendGroupInvitePatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SPTarkov.Server.Core.Callbacks.MatchCallbacks), nameof(SPTarkov.Server.Core.Callbacks.MatchCallbacks.SendGroupInvite));

        public SendGroupInvitePatch(RaidController raidController)
        {
            _raidController = raidController;
        }

        private static RaidController _raidController;

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