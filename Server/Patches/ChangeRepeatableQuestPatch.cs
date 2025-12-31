
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Quests;
using SPTarkov.Server.Core.Models.Spt.Quests;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Patches
{
    public sealed class ChangeRepeatableQuestPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RepeatableQuestController), nameof(RepeatableQuestController.ChangeRepeatableQuest));

        [PatchPrefix]
        public static bool Prefix(RepeatableQuestController __instance, PmcData pmcData, RepeatableQuestChangeRequest changeRequest, MongoId sessionID, ref ItemEventRouterResponse __result)
        {
            var eventOutputHolder = ServiceLocator.ServiceProvider.GetService<EventOutputHolder>();
            var serverLocalisationService = ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>();
            var httpResponseUtil = ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>();
            var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<RepeatableQuestChangeRequest>>();
            
            var output = eventOutputHolder.GetOutput(sessionID);

            var repeatableQuestControllerTraverse = Traverse.Create(__instance);
            var repeatables = repeatableQuestControllerTraverse.Method("GetRepeatableById", [changeRequest.QuestId, pmcData]).GetValue<GetRepeatableByIdResult?>();
            var questToReplace = repeatables.Quest;
            if (repeatables.RepeatableType is null || repeatables.Quest is null)
            {
                var message = serverLocalisationService.GetText("quest-unable_to_find_repeatable_to_replace");
                logger.Error(message);

                __result = httpResponseUtil.AppendErrorToOutput(output, message);
                return false;
            }

            if (questToReplace.SptRepatableGroupName == "Order")
            {
                var message = serverLocalisationService.GetText("quest-unable_to_find_repeatable_to_replace");
                logger.Warning(message);

                __result = output;
                return false;
            }

            return true;
        }
    }
}