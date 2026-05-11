
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

        [PatchPostfix]
        public static void Postfix(MongoId sessionId, MongoId traderId, ref TraderAssort __result)
        {
            if (traderId != Services.TraderService.MiyakoTraderId)
            {
                return;
            }

            var profileController = ServiceLocator.ServiceProvider.GetService<Controllers.ProfileController>();
            if (!profileController.IsMcsBotPlayerInventoryMode(sessionId))  
            {  
                return;
            }  

            var traderController = ServiceLocator.ServiceProvider.GetService<Controllers.TraderController>();
            var traderAssort = traderController.GetMcsBotPlayerInventoryModeAssort();
            // __result = traderController.GetMcsBotPlayerInventoryModeAssort();

            if (profileController.IsMcsBotPlayerInventoryMode(sessionId))
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