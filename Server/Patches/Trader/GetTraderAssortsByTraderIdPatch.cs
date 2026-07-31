
using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Traders;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace MiyakoCarryService.Server.Patches.Trader
{
    /// <summary>
    /// 处于护航库存模式时，使宫子商人能够正常交易
    /// </summary>
    [Injectable]
    public sealed class GetTraderAssortsByTraderIdPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TraderHelper), nameof(TraderHelper.GetTraderAssortsByTraderId));

        public GetTraderAssortsByTraderIdPatch(Controllers.TraderController traderController)
        {
            _traderController = traderController;
        }

        private static Controllers.TraderController _traderController;

        [PatchPrefix]  
        public static bool Prefix(MongoId traderId, ref TraderAssort? __result)  
        {  
            if (traderId != Services.TraderService.MiyakoTraderId)
            {
                return true;
            }

            __result = _traderController.GetMcsBotPlayerInventoryModeAssort();
            return false;
        }
    }
}