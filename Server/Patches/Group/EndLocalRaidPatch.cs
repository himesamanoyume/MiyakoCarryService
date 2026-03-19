
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
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
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchController), nameof(MatchController.EndLocalRaid));

        [PatchPrefix]
        public static void Prefix(MongoId sessionId, EndLocalRaidRequestData request)
        {
            var isTransfer = request.Results.IsMapToMapTransfer();
            System.Console.WriteLine($"[Mcs-Debug] 本次战局结束, 撤离状态: {request.Results.Result}");
            if (isTransfer)
            {
                return;
            }
            System.Console.WriteLine($"[Mcs-Debug] 进行护航小队清理");
            var raidController = ServiceLocator.ServiceProvider.GetService<RaidController>();
            raidController.ClearGroupMember(sessionId);
        }
    }
}