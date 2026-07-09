using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Generators.Ragfair;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace MiyakoCarryService.Server.Patches.Trader
{
    /// <summary>  
    /// 生成跳蚤市场供货时，宫子商人为护航库存模式提供全物品购买 
    /// </summary>  
    public sealed class GenerateFleaOffersForTraderPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RagfairOfferGenerator), nameof(RagfairOfferGenerator.GenerateFleaOffersForTrader));

        public GenerateFleaOffersForTraderPatch(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        private static IServiceProvider ServiceProvider;

        private static TradersTable TradersTable { get => field ??= ServiceProvider.GetService<TradersTable>(); }
        private static Controllers.TraderController TraderController { get => field ??= ServiceProvider.GetService<Controllers.TraderController>(); }

        [PatchPrefix]
        public static void Prefix(MongoId traderId)
        {
            if (traderId != Services.TraderService.MiyakoTraderId)
            {
                return;
            }

            if (!TradersTable.TryGetValue(traderId, out var trader))
            {
                return;
            }

            trader.Assort = TraderController.GetMcsBotPlayerInventoryModeAssort();
        }

        [PatchPostfix]
        public static void Postfix(MongoId traderId)
        {
            if (traderId != Services.TraderService.MiyakoTraderId)
            {
                return;
            }

            if (!TradersTable.TryGetValue(traderId, out var trader))
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