
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;

namespace MiyakoCarryService.Server.Patches
{
    public sealed class SendGroupInvitePatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchCallbacks), nameof(MatchCallbacks.SendGroupInvite));

        [PatchPrefix]
        public static void Prefix(string url, MatchGroupInviteSendRequest info, MongoId sessionID)
        {
            var csAccountId = info.To;
            var mcsRaidCController = ServiceLocator.ServiceProvider.GetService<MCSRaidController>();
            var check = int.TryParse(csAccountId, out var aid);
            if (!check)
            {
                return;
            }
            mcsRaidCController.AcceptGroupInvite(sessionID, aid);
        }
    }
}