
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace MiyakoCarryService.Server.Patches
{
    public sealed class GameStartPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameCallbacks), nameof(GameCallbacks.GameStart));

        [PatchPrefix]
        public static void Prefix(string url, EmptyRequestData _, MongoId sessionID)
        {
            var raidController = ServiceLocator.ServiceProvider.GetService<RaidController>();
            raidController.ClearGroupMember(sessionID);
        }
    }
}