
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
    public sealed class RemovePlayerFromGroupPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchCallbacks), nameof(MatchCallbacks.RemovePlayerFromGroup));

        [PatchPrefix]
        public static void Prefix(string url, MatchGroupPlayerRemoveRequest info, MongoId sessionID)
        {
            var raidController = ServiceLocator.ServiceProvider.GetService<RaidController>();
            var check = int.TryParse(info.AidToKick, out var mcsAid);
            if (!check)
            {
                return;
            }
            raidController.RemoveGroupMember(sessionID, mcsAid);
        }
    }
}