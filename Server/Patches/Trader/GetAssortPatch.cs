
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace MiyakoCarryService.Server.Patches.Trader
{
    /// <summary>
    /// 处于护航库存模式时，宫子商人提供全物品购买
    /// </summary>
    public sealed class GetAssortPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TraderAssortHelper), nameof(TraderAssortHelper.GetAssort));

        private static Controllers.ProfileController ProfileController { get => field ??= ServiceLocator.ServiceProvider.GetService<Controllers.ProfileController>(); }
        private static Controllers.TraderController TraderController { get => field ??= ServiceLocator.ServiceProvider.GetService<Controllers.TraderController>(); }

        [PatchPostfix]
        public static void Postfix(MongoId sessionId, MongoId traderId, ref TraderAssort __result)
        {
            if (traderId != Services.TraderService.MiyakoTraderId)
            {
                return;
            }

            if (!ProfileController.IsMcsBotPlayerInventoryMode(sessionId))  
            {  
                return;
            }  

            var traderAssort = TraderController.GetMcsBotPlayerInventoryModeAssort();

            if (ProfileController.IsMcsBotPlayerInventoryMode(sessionId))
            {
                __result = traderAssort;
            }
            else
            {
                __result = new TraderAssort
                {
                    Items = traderAssort.Items,
                    BarterScheme = new(),
                    LoyalLevelItems = new()
                };
            }
        }
    }
}