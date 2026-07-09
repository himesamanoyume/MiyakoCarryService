
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Patches.Trader
{
    /// <summary>
    /// 处于护航库存模式时，阻止上架跳蚤市场
    /// </summary>
    public sealed class AddOfferPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RagfairController), nameof(RagfairController.AddPlayerOffer));

        private static EventOutputHolder EventOutputHolder { get => field ??= ServiceLocator.ServiceProvider.GetService<EventOutputHolder>(); }
        private static HttpResponseUtil HttpResponseUtil { get => field ??= ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>(); }
        private static ServerLocalisationService ServerLocalisationService { get => field ??= ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>(); }
        private static Controllers.ProfileController ProfileController { get => field ??= ServiceLocator.ServiceProvider.GetService<Controllers.ProfileController>(); }

        [PatchPrefix]  
        public static bool Prefix(PmcData pmcData, AddOfferRequestData offerRequest, MongoId sessionID, ref ItemEventRouterResponse __result)  
        {  
            var output = EventOutputHolder.GetOutput(sessionID);
            if (ProfileController.IsMcsBotPlayerInventoryMode(sessionID))  
            {
                __result = HttpResponseUtil.AppendErrorToOutput(output, ServerLocalisationService.GetText(Locales.MCSINVENTORYMODERAGFAIRREFUSE));
                return false;
            }  
            return true;
        }
    }
}