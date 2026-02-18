
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;

namespace MiyakoCarryService.Server.Patches.Trader
{
    /// <summary>
    /// 防止宫子商人的Standing值最小不得低于0
    /// </summary>
    public sealed class AddStandingToTraderPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TraderHelper), nameof(TraderHelper.AddStandingToTrader));

        [PatchPrefix]
        public static bool Prefix(MongoId sessionId, MongoId traderId, double standingToAdd)
        {
            if (traderId == TraderService.MiyakoTraderId)
            {
                var traderController = ServiceLocator.ServiceProvider.GetService<TraderController>();
                traderController.AddTraderStanding(sessionId, standingToAdd);
                return false;
            }
            return true;
        }
    }
}