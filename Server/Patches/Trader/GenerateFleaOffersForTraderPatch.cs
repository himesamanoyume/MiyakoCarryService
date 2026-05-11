using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace MiyakoCarryService.Server.Patches.Trader
{
    /// <summary>  
    /// 生成跳蚤市场供货时，宫子商人为护航库存模式提供全物品购买 
    /// </summary>  
    public sealed class GenerateFleaOffersForTraderPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RagfairOfferGenerator), nameof(RagfairOfferGenerator.GenerateFleaOffersForTrader));

        [PatchPrefix]
        public static void Prefix(MongoId traderId)
        {
            if (traderId != Services.TraderService.MiyakoTraderId)
            {
                return;
            }

            var databaseService = ServiceLocator.ServiceProvider.GetService<DatabaseService>();
            var traderController = ServiceLocator.ServiceProvider.GetService<Controllers.TraderController>();

            if (!databaseService.GetTables().Traders.TryGetValue(traderId, out var trader))
            {
                return;
            }

            trader.Assort = traderController.GetMcsBotPlayerInventoryModeAssort();
        }

        [PatchPostfix]
        public static void Postfix(MongoId traderId)
        {
            if (traderId != Services.TraderService.MiyakoTraderId)
            {
                return;
            }

            var databaseService = ServiceLocator.ServiceProvider.GetService<DatabaseService>();

            if (!databaseService.GetTables().Traders.TryGetValue(traderId, out var trader))
            {
                return;
            }

            trader.Assort = new TraderAssort
            {
                Items = [],
                BarterScheme = new(),
                LoyalLevelItems = new()
            };
        }
    }
}