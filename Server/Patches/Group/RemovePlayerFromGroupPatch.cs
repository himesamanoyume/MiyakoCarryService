
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;

namespace MiyakoCarryService.Server.Patches.Group
{
    /// <summary>
    /// 实现玩家将护航移除出小队时，服务端一并将其从小队中移除
    /// </summary>
    [Injectable]
    public sealed class RemovePlayerFromGroupPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchCallbacks), nameof(MatchCallbacks.RemovePlayerFromGroup));

        public RemovePlayerFromGroupPatch(RaidController raidController)
        {
            _raidController = raidController;
        }

        private static RaidController _raidController;

        [PatchPrefix]
        public static void Prefix(string url, MatchGroupPlayerRemoveRequest info, MongoId sessionID)
        {
            var check = int.TryParse(info.AidToKick, out var mcsAid);
            if (!check)
            {
                return;
            }
            _raidController.RemoveGroupMember(sessionID, mcsAid);
        }
    }
}