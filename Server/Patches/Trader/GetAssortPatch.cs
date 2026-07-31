
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
    /// 处于护航库存模式时，宫子商人提供全物品购买
    /// </summary>
    [Injectable]
    public sealed class GetAssortPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TraderAssortHelper), nameof(TraderAssortHelper.GetAssort));

        public GetAssortPatch(Controllers.ProfileController profileController, Controllers.TraderController traderController)
        {
            _profileController = profileController;
            _traderController = traderController;
        }

        private static Controllers.ProfileController _profileController;
        private static Controllers.TraderController _traderController;

        [PatchPostfix]
        public static void Postfix(MongoId sessionId, MongoId traderId, ref TraderAssort __result)
        {
            if (traderId != Services.TraderService.MiyakoTraderId)
            {
                return;
            }

            if (!_profileController.IsMcsBotPlayerInventoryMode(sessionId))  
            {  
                return;
            }  

            var traderAssort = _traderController.GetMcsBotPlayerInventoryModeAssort();

            if (_profileController.IsMcsBotPlayerInventoryMode(sessionId))
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