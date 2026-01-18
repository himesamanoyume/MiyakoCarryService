
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
            var mcsAid = info.To;
            var raidController = ServiceLocator.ServiceProvider.GetService<RaidController>();
            var isInt = int.TryParse(mcsAid, out var iMcsAid);
            if (!isInt)
            {
                return;
            }
            raidController.AcceptGroupInvite(sessionID, iMcsAid);
        }
    }
}