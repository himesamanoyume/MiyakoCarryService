
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Patches.Trader
{
    /// <summary>
    /// 处于护航库存模式时，阻止上架跳蚤市场
    /// </summary>
    [Injectable]
    public sealed class AddOfferPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RagfairController), nameof(RagfairController.AddPlayerOffer));

        public AddOfferPatch(EventOutputHolder eventOutputHolder, HttpResponseUtil httpResponseUtil, ServerLocalisationService serverLocalisationService, Controllers.ProfileController profileController)
        {
            _eventOutputHolder = eventOutputHolder;
            _httpResponseUtil = httpResponseUtil;
            _serverLocalisationService = serverLocalisationService;
            _profileController = profileController;
        }

        private static EventOutputHolder _eventOutputHolder;
        private static HttpResponseUtil _httpResponseUtil;
        private static ServerLocalisationService _serverLocalisationService;
        private static Controllers.ProfileController _profileController;

        [PatchPrefix]  
        public static bool Prefix(PmcData pmcData, AddOfferRequestData offerRequest, MongoId sessionID, ref ItemEventRouterResponse __result)  
        {  
            var output = _eventOutputHolder.GetOutput(sessionID);
            if (_profileController.IsMcsBotPlayerInventoryMode(sessionID))  
            {
                __result = _httpResponseUtil.AppendErrorToOutput(output, _serverLocalisationService.GetText(Locales.MCSINVENTORYMODERAGFAIRREFUSE));
                return false;
            }  
            return true;
        }
    }
}