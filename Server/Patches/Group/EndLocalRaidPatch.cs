
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

namespace MiyakoCarryService.Server.Patches.Group
{
    /// <summary>
    /// 战局结束时如果类型不是转移，则清空该玩家的小队成员
    /// </summary>
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
            var raidController = ServiceLocator.ServiceProvider.GetService<RaidController>();
            raidController.ClearGroupMember(sessionID);
        }
    }
}