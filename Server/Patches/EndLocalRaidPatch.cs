
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;

namespace MiyakoCarryService.Server.Patches
{
    public sealed class EndLocalRaidPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchCallbacks), nameof(MatchCallbacks.EndLocalRaid));

        [PatchPrefix]
        public static void Prefix(string url, EndLocalRaidRequestData info, MongoId sessionID)
        {
            var isTransfer = info.Results.IsMapToMapTransfer();
            if (isTransfer)
            {
                return;
            }
            var mcsRaidController = ServiceLocator.ServiceProvider.GetService<MCSRaidController>();
            mcsRaidController.ClearGroupMember(sessionID);
        }
    }
}