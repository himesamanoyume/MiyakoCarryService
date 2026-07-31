using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
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
    [Injectable]
    public sealed class GenerateFleaOffersForTraderPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RagfairOfferGenerator), nameof(RagfairOfferGenerator.GenerateFleaOffersForTrader));

        public GenerateFleaOffersForTraderPatch(TradersTable tradersTable, Controllers.TraderController traderController)
        {
            _tradersTable = tradersTable;
            _traderController = traderController;
        }

        private static TradersTable _tradersTable;
        private static Controllers.TraderController _traderController;

        [PatchPrefix]
        public static void Prefix(MongoId traderId)
        {
            if (traderId != Services.TraderService.MiyakoTraderId)
            {
                return;
            }

            if (!_tradersTable.TryGetValue(traderId, out var trader))
            {
                return;
            }

            trader.Assort = _traderController.GetMcsBotPlayerInventoryModeAssort();
        }

        [PatchPostfix]
        public static void Postfix(MongoId traderId)
        {
            if (traderId != Services.TraderService.MiyakoTraderId)
            {
                return;
            }

            if (!_tradersTable.TryGetValue(traderId, out var trader))
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