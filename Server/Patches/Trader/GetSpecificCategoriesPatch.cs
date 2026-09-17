using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Ragfair;

namespace MiyakoCarryService.Server.Patches.Trader
{
    [Injectable]
    public sealed class GetSpecificCategoriesPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RagfairController), "GetSpecificCategories");

        public GetSpecificCategoriesPatch(Controllers.ProfileController profileController, GlobalTable globalTable, RagfairOfferService ragfairOfferService, RagfairCategoriesService ragfairCategoriesService
        )
        {
            _profileController = profileController;
            _globalTable = globalTable;
            _ragfairOfferService = ragfairOfferService;
            _ragfairCategoriesService = ragfairCategoriesService;
        }

        private static Controllers.ProfileController _profileController;
        private static GlobalTable _globalTable;
        private static RagfairOfferService _ragfairOfferService;
        private static RagfairCategoriesService _ragfairCategoriesService;

        [PatchPrefix]
        public static bool Prefix(
            PmcData pmcProfile,
            SearchRequestData searchRequest,
            List<RagfairOffer> offers,
            ref Dictionary<MongoId, int> __result
        )
        {
            if (pmcProfile?.Id is null || _profileController.IsMcsBotPlayerInventoryMode(pmcProfile.Id.Value))
            {
                return true;
            }

            var playerHasFleaUnlocked = pmcProfile.Info.Level >= _globalTable.Configuration.RagFair.MinUserLevel;

            var isLinkedOrRequiredSearch = !string.IsNullOrEmpty(searchRequest.LinkedSearchId) || !string.IsNullOrEmpty(searchRequest.NeededSearchId);
            var offerPool = isLinkedOrRequiredSearch ? offers : _ragfairOfferService.GetOffers();

            MongoId? miyakoTraderId = new MongoId(Services.TraderService.MiyakoTraderId);

            __result = _ragfairCategoriesService.GetCategoriesFromOffers(
                offerPool.Where(offer => offer.User.Id != miyakoTraderId),
                searchRequest,
                playerHasFleaUnlocked
            );

            return false;
        }
    }
}