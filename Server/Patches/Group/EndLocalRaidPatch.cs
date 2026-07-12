
using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
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
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchController), nameof(MatchController.EndLocalRaidAsync));

        public EndLocalRaidPatch(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        private static IServiceProvider ServiceProvider;

        private static RaidController RaidController { get => field ??= ServiceProvider.GetService<RaidController>(); }

        private static RaidController RaidController { get => field ??= ServiceLocator.ServiceProvider.GetService<RaidController>(); }

        [PatchPrefix]
        public static void Prefix(MongoId sessionId, EndLocalRaidRequestData request)
        {
            var isTransfer = request.Results.IsMapToMapTransfer();
            if (isTransfer)
            {
                return;
            }
            RaidController.ClearGroupMember(sessionId);
        }
    }
}